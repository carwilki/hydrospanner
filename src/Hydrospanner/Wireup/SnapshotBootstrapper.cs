namespace Hydrospanner.Wireup
{
	using System;
	using System.Threading;
	using log4net;
	using Persistence;
	using Phases.Bootstrap;
	using Phases.Snapshot;

	internal class SnapshotBootstrapper
	{
		public virtual BootstrapInfo RestoreSnapshots(IRepository repository, BootstrapInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (repository == null)
				throw new ArgumentNullException("repository");

			if (info.JournaledSequence == 0)
				return info;

			using (var reader = this.snapshotFactory.CreateSystemSnapshotStreamReader(info.JournaledSequence))
			{
				if (reader.MessageSequence == 0)
					return info;

				if (reader.Count == 0)
					return info.AddSnapshotSequence(reader.MessageSequence);

				Log.InfoFormat(
					"Restoring {0} mementos from the snapshot at message sequence {1} (this could take some time...).", 
					reader.Count, 
					reader.MessageSequence);

				using (var disruptor = this.disruptorFactory.CreateBootstrapDisruptor(repository, reader.Count, this.OnComplete))
				{
					Publish(reader, disruptor.Start());
					this.mutex.WaitOne();
					return this.success ? info.AddSnapshotSequence(reader.MessageSequence) : null;
				}	
			}
		}
		private void OnComplete(bool result)
		{
			this.success = result;
			this.mutex.Set();
		}
		private static void Publish(SystemSnapshotStreamReader reader, IRingBuffer<BootstrapItem> ring)
		{
			foreach (var memento in reader.Read())
			{
				var next = ring.Next();
				var claimed = ring[next];
				claimed.AsSnapshot(memento.Key, memento.Value);
				ring.Publish(next);
			}
		}

		public virtual void SaveSnapshot(IRepository repository, IRingBuffer<SnapshotItem> ringBuffer, BootstrapInfo info)
		{
			TakePublicSnapshot(repository, ringBuffer);
			this.TryTakeSystemSnapshot(repository, ringBuffer, info); // this must *always* happen after public snapshots
		}
		private static void TakePublicSnapshot(IRepository repository, IRingBuffer<SnapshotItem> ringBuffer)
		{
			var count = 0;
			var hydratables = repository.Accessed;
			foreach (var pair in hydratables)
			{
				var hydratable = pair.Key as IPublicHydratable;
				if (hydratable == null)
					continue;

				count++;
				var claimed = ringBuffer.Next();
				var item = ringBuffer[claimed];
				item.AsPublicSnapshot(hydratable.Key, hydratable.Memento, hydratable.MementoType, pair.Value);
				ringBuffer.Publish(claimed);
			}

			Log.InfoFormat("{0} public hydratables snapshots taken.", count);
			hydratables.TryClear();
		}
		private void TryTakeSystemSnapshot(IRepository repository, IRingBuffer<SnapshotItem> ringBuffer, BootstrapInfo info)
		{
			if ((info.JournaledSequence - info.SnapshotSequence) <= this.snapshotFrequency)
				return;

			var items = repository.Items;
			var remaining = items.Count;
			foreach (var hydratable in items)
			{
				var next = ringBuffer.Next();
				var claimed = ringBuffer[next];
				claimed.AsPartOfSystemSnapshot(info.JournaledSequence, --remaining, hydratable.Memento);
				ringBuffer.Publish(next);
			}
		}

		public SnapshotBootstrapper(SnapshotFactory snapshotFactory, DisruptorFactory disruptorFactory, long snapshotFrequency)
		{
			if (snapshotFactory == null)
				throw new ArgumentNullException("snapshotFactory");

			if (disruptorFactory == null)
				throw new ArgumentNullException("disruptorFactory");

			if (snapshotFrequency <= 0)
				throw new ArgumentOutOfRangeException("snapshotFrequency");

			this.snapshotFactory = snapshotFactory;
			this.disruptorFactory = disruptorFactory;
			this.snapshotFrequency = snapshotFrequency;
		}
		protected SnapshotBootstrapper()
		{
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(SnapshotBootstrapper));
		private readonly AutoResetEvent mutex = new AutoResetEvent(false);
		private readonly SnapshotFactory snapshotFactory;
		private readonly DisruptorFactory disruptorFactory;
		private readonly long snapshotFrequency;
		private bool success;
	}
}