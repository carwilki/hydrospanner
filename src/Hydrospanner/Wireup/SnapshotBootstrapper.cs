namespace Hydrospanner.Wireup
{
	using System;
	using System.Threading;
	using log4net;
	using Persistence;
	using Phases.Bootstrap;
	using Phases.Snapshot;

	public class SnapshotBootstrapper
	{
		public virtual BootstrapInfo RestoreSnapshots(BootstrapInfo info, IRepository repository)
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
					"Restoring {0} mementos from the snapshot at generation {1}, message sequence {2} (this could take some time...).", 
					reader.Count, 
					reader.Generation, 
					reader.MessageSequence);

				using (var disruptor = this.disruptorFactory.CreateBootstrapDisruptor(repository, reader.Count, () => this.mutex.Set()))
				{
					Publish(reader, disruptor.Start());
					this.mutex.WaitOne();
					return info.AddSnapshotSequence(reader.MessageSequence);
				}	
			}
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
	}
}