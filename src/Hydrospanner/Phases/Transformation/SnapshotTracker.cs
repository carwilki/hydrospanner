﻿namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Linq;
	using Snapshot;

	public sealed class SnapshotTracker : ISnapshotTracker
	{
		public void Increment(int messages)
		{
			this.currentSequence += messages;

			if (this.currentSequence < this.nextSnapshotSequence)
				return;

			this.PublishMementos();
			this.CalculateNextSnapshotSequence();
		}
		private void PublishMementos()
		{
			var mementos = this.repository.GetMementos().ToArray();

			for (var i = mementos.Length; i-- > 0;)
			{
				var next = this.snapshotRing.Next();
				var claimed = this.snapshotRing[next];
				claimed.AsPartOfSystemSnapshot(this.currentSequence, i, mementos[i]);
				this.snapshotRing.Publish(next);
			}
		}
		private void CalculateNextSnapshotSequence()
		{
			this.nextSnapshotSequence = ((this.currentSequence / this.frequency) * this.frequency) + this.frequency;
		}

		public SnapshotTracker(long journaledSequence, int frequency, IRingBuffer<SnapshotItem> snapshotRing, IRepository repository)
		{
			if (journaledSequence < 0)
				throw new ArgumentOutOfRangeException("journaledSequence");

			if (frequency < 100)
				throw new ArgumentOutOfRangeException("frequency");

			if (snapshotRing == null)
				throw new ArgumentNullException("snapshotRing");

			if (repository == null)
				throw new ArgumentNullException("repository");

			this.currentSequence = journaledSequence;
			this.frequency = frequency;
			this.CalculateNextSnapshotSequence();
			this.snapshotRing = snapshotRing;
			this.repository = repository;
		}

		private readonly long frequency;
		private readonly IRingBuffer<SnapshotItem> snapshotRing;
		private readonly IRepository repository;
		private long currentSequence;
		private long nextSnapshotSequence;
	}
}