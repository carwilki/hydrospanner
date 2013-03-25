namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using System.Threading;
	using Configuration;
	using Disruptor;
	using Persistence;
	using Snapshot;

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

				using (var disruptor = this.disruptorFactory.CreateBootstrapDisruptor(repository, reader.Count, () => this.mutex.Set()))
				{
					Publish(reader, disruptor.Start());
					this.mutex.WaitOne();
					return info.AddSnapshotSequence(reader.MessageSequence);
				}	
			}
		}
		private static void Publish(SystemSnapshotStreamReader reader, RingBuffer<BootstrapItem> ring)
		{
			foreach (var memento in reader.Read())
			{
				var m = memento;
				ring.Publish(item => item.AsSnapshot(m.Key, m.Value));
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

		private readonly AutoResetEvent mutex = new AutoResetEvent(false);
		private readonly SnapshotFactory snapshotFactory;
		private readonly DisruptorFactory disruptorFactory;
	}
}