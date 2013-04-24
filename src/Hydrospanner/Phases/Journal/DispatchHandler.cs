namespace Hydrospanner.Phases.Journal
{
	using System;
	using System.Collections.Generic;
	using Disruptor;
	using log4net;
	using Messaging;

	public sealed class DispatchHandler : IEventHandler<JournalItem>
	{
		public void OnNext(JournalItem data, long sequence, bool endOfBatch)
		{
			if (data.ItemActions.HasFlag(JournalItemAction.Dispatch))
			{
				// TODO: get this if statement under test
				if (!(data.Body is IInternalMessage))
				{
					Log.DebugFormat("Received journal item of type {0} for dispatch.", data.SerializedType);
					this.buffer.Add(data);
				}
			}

			if (endOfBatch)
				this.TryDispatch();
		}
		private void TryDispatch()
		{
			try
			{
				while (true)
					if (this.Dispatch())
						break;
			}
			catch (ObjectDisposedException)
			{
			}
			finally
			{
				this.buffer.Clear();
			}
		}
		public bool Dispatch()
		{
			if (this.buffer.Count == 0)
				return true;

			Log.InfoFormat("Dispatching {0} items.", this.buffer.Count);
			for (var i = 0; i < this.buffer.Count; i++)
				if (!this.Dispatch(this.buffer[i]))
					return false;
			
			return this.sender.Commit();
		}
		private bool Dispatch(JournalItem item)
		{
			if (this.sender.Send(item))
				return true;

			Log.WarnFormat("Failed to dispatch message sequence {0} of type {1}.", item.MessageSequence, item.SerializedType);
			return false;
		}

		public DispatchHandler(IMessageSender sender)
		{
			this.sender = sender;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(DispatchHandler));
		private readonly List<JournalItem> buffer = new List<JournalItem>(1024 * 32);
		private readonly IMessageSender sender;
	}
}