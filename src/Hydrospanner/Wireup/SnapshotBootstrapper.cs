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

		public virtual void SavePublicSnapshots(IRepository repository, IRingBuffer<SnapshotItem> ringBuffer, long sequence)
		{
			foreach (var hydratable in repository)
			{
				if (!hydratable.IsPublicSnapshot)
					continue;

				var memento = hydratable.Memento;
				var cloner = memento as ICloneable;
				memento = (cloner == null ? memento : cloner.Clone()) ?? memento;

				var claimed = ringBuffer.Next();
				var item = ringBuffer[claimed];
				item.AsPublicSnapshot(hydratable.Key, memento, sequence);
				ringBuffer.Publish(claimed);
			}
		}

		public SnapshotBootstrapper(SnapshotFactory snapshotFactory, DisruptorFactory disruptorFactory)
		{
			if (snapshotFactory == null)
				throw new ArgumentNullException("snapshotFactory");

			if (disruptorFactory == null)
				throw new ArgumentNullException("disruptorFactory");

			this.snapshotFactory = snapshotFactory;
			this.disruptorFactory = disruptorFactory;
		}
		protected SnapshotBootstrapper()
		{
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(SnapshotBootstrapper));
		private readonly AutoResetEvent mutex = new AutoResetEvent(false);
		private readonly SnapshotFactory snapshotFactory;
		private readonly DisruptorFactory disruptorFactory;
		private bool success;
	}
}