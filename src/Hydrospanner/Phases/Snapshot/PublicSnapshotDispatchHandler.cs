namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using Disruptor;
	using log4net;
	using Messaging;

	public class PublicSnapshotDispatchHandler : IEventHandler<SnapshotItem>
	{
		public void OnNext(SnapshotItem data, long sequence, bool endOfBatch)
		{
			if (data.IsPublicSnapshot)
				this.buffer[data.Key] = data;

			if (endOfBatch && this.buffer.Count > 0)
				this.TryDispatch();
		}
		private void TryDispatch()
		{
			Log.InfoFormat("Dispatching {0} public snapshots.", this.buffer.Count);

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
			foreach (var item in this.buffer)
				if (!this.Dispatch(item.Value))
					return false;

			return this.sender.Commit();
		}
		private bool Dispatch(SnapshotItem item)
		{
			if (this.sender.Send(item))
				return true;

			Log.WarnFormat("Failed to dispatch message sequence {0} of type {1}.", item.CurrentSequence, item.Memento.ResolvableTypeName());
			return false;
		}

		public PublicSnapshotDispatchHandler(IMessageSender sender)
		{
			this.sender = sender;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(PublicSnapshotDispatchHandler));
		private readonly Dictionary<string, SnapshotItem> buffer = new Dictionary<string, SnapshotItem>();
		private readonly IMessageSender sender;
	}
}