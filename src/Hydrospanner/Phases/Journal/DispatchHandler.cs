﻿namespace Hydrospanner.Phases.Journal
{
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
				Log.DebugFormat("Received journal item of type {0} for dispatch.", data.SerializedType);
				this.buffer.Add(data);
			}

			if (!endOfBatch)
				return;

			while (true)
				if (this.Dispatch())
					break;

			this.buffer.Clear();
		}
		public bool Dispatch()
		{
			if (this.buffer.Count == 0)
				return true;

			Log.InfoFormat("Dispatching {0} items.", this.buffer.Count);
			for (var i = 0; i < this.buffer.Count; i++)
				if (!this.sender.Send(this.buffer[i]))
				{
					Log.WarnFormat(
						"Failed to dispatch message sequence {0} of type {1}.", 
						this.buffer[i].MessageSequence, 
						this.buffer[i].SerializedType);

					return false;
				}
			
			return this.sender.Commit();
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