namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Disruptor.Dsl;
	using Journal;
	using Persistence;
	using Persistence.SqlPersistence;
	using Snapshot;

	internal class Ignition
	{
		// TODO: this is just spike code for now...

		public void Start()
		{
			var info = this.bootstrapStore.Load();
			var snapshot = this.snapshotLoader.Load(info.JournaledSequence, this.snapshotIteration);
			info = info.AddSnapshotSequence(snapshot.MessageSequence);

			this.EngageDisruptors();

			this.DispatchPendingMessages(info);
			this.LoadFromSnapshot(snapshot);
			this.ReplayFromCheckpoint(info);
		}

		void EngageDisruptors()
		{
			this.dispatchRing.Start();
			this.snapshotRing.Start();
			this.bootstrapRing.Start();
		}

		void DispatchPendingMessages(BootstrapInfo info)
		{
			foreach (var message in this.messageStore.LoadFrom(info.DispatchSequence))
			{
				var next = this.dispatchRing.RingBuffer.Next();
				var claimed = this.dispatchRing.RingBuffer[next];
				claimed.AsBootstrappedDispatchMessage(
					message.Sequence, message.SerializedBody, message.TypeName, message.SerializedHeaders);
				this.dispatchRing.RingBuffer.Publish(next);
			}
		}

		void LoadFromSnapshot(SystemSnapshotStreamReader snapshot)
		{
			var batch = this.bootstrapRing.RingBuffer.NewBatchDescriptor(snapshot.Count);
			var index = batch.Start;

			foreach (var memento in snapshot.Read())
				this.bootstrapRing.RingBuffer[index++].AsSnapshot(memento.Key, memento.Value);
			
			this.bootstrapRing.RingBuffer.Publish(batch);
		}

		void ReplayFromCheckpoint(BootstrapInfo info)
		{
			foreach (var message in this.messageStore.LoadFrom(Math.Min(info.SnapshotSequence, info.JournaledSequence)))
			{
				var next = this.bootstrapRing.RingBuffer.Next();
				var claimed = this.bootstrapRing.RingBuffer[next];
				claimed.AsReplayMessage(message.Sequence, message.TypeName, message.SerializedBody, message.SerializedHeaders);
				this.bootstrapRing.RingBuffer.Publish(next);
			}
		}

		public Ignition(
			Disruptor<BootstrapItem> bootstrapRing,
			Disruptor<SnapshotItem> snapshotRing,
			Disruptor<JournalItem> dispatchRing,
			SqlBootstrapStore bootstrapStore,
			SqlMessageStore messageStore,
			SystemSnapshotLoader snapshotLoader,
			int snapshotIteration)
		{
			this.bootstrapRing = bootstrapRing;
			this.snapshotRing = snapshotRing;
			this.dispatchRing = dispatchRing;
			this.bootstrapStore = bootstrapStore;
			this.messageStore = messageStore;
			this.snapshotLoader = snapshotLoader;
			this.snapshotIteration = snapshotIteration;
		}

		readonly Disruptor<BootstrapItem> bootstrapRing;
		readonly Disruptor<SnapshotItem> snapshotRing;
		readonly Disruptor<JournalItem> dispatchRing;
		readonly SqlBootstrapStore bootstrapStore;
		readonly SqlMessageStore messageStore;
		readonly SystemSnapshotLoader snapshotLoader;
		readonly int snapshotIteration;
	}
}