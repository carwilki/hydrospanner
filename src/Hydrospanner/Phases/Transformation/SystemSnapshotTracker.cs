namespace Hydrospanner.Phases.Transformation
{
	using System;
	using Snapshot;

	public sealed class SystemSnapshotTracker : ISystemSnapshotTracker
	{
		public void Track(long sequence)
		{
			if (sequence < this.nextSnapshotSequence) 
				return;
			
			this.PublishMementos(sequence);
			
			while (sequence >= this.nextSnapshotSequence)
				this.nextSnapshotSequence += this.frequency;
		}

		private void PublishMementos(long sequence)
		{
			var items = this.repository.Items;
			var remaining = items.Count;
			foreach (var hydratable in items)
			{
				var next = this.snapshotRing.Next();
				var claimed = this.snapshotRing[next];
				claimed.AsPartOfSystemSnapshot(sequence, --remaining, hydratable.Memento);
				this.snapshotRing.Publish(next);
			}
		}

		public SystemSnapshotTracker(long journaledSequence, int frequency, IRingBuffer<SnapshotItem> snapshotRing, IRepository repository)
		{
			if (journaledSequence < 0)
				throw new ArgumentOutOfRangeException("journaledSequence");

			if (frequency < 100)
				throw new ArgumentOutOfRangeException("frequency");

			if (snapshotRing == null)
				throw new ArgumentNullException("snapshotRing");

			if (repository == null)
				throw new ArgumentNullException("repository");

			this.frequency = frequency;
			this.nextSnapshotSequence = ((journaledSequence / this.frequency) * this.frequency) + this.frequency;
			this.snapshotRing = snapshotRing;
			this.repository = repository;
		}

		private readonly long frequency;
		private readonly IRingBuffer<SnapshotItem> snapshotRing;
		private readonly IRepository repository;
		private long nextSnapshotSequence;
	}

	public sealed class NullSystemSnapshotTracker : ISystemSnapshotTracker
	{
		public void Track(long sequence)
		{
		}
	}
}