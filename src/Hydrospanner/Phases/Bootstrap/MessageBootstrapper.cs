namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Configuration;
	using Journal;
	using Persistence;
	using Transformation;

	public class MessageBootstrapper
	{
		public virtual void Restore(BootstrapInfo info, IDisruptor<JournalItem> journalRing, IRepository repository)
		{
			ValidateInput(info, journalRing, repository);
			this.RestoreFrom(info, journalRing, repository);
			this.Shutdown();
		}

		private static void ValidateInput(BootstrapInfo info, IDisruptor<JournalItem> journalRing, IRepository repository)
		{
			if (info == null)
				throw new ArgumentNullException("info");
			
			if (journalRing == null) 
				throw new ArgumentNullException("journalRing");
			
			if (repository == null) 
				throw new ArgumentNullException("repository");
		}

		private void RestoreFrom(BootstrapInfo info, IDisruptor<JournalItem> journalRing, IRepository repository)
		{
			foreach (var message in this.store.Load(Math.Min(info.DispatchSequence, info.SnapshotSequence)))
			{
				if (message.Sequence > info.DispatchSequence)
				{
					var next = journalRing.RingBuffer.Next();
					var claimed = journalRing.RingBuffer[next];
					claimed.AsBootstrappedDispatchMessage(message.Sequence, message.SerializedBody, message.SerializedType, message.SerializedHeaders);
					journalRing.RingBuffer.Publish(next);
				}

				if (message.Sequence > info.SnapshotSequence)
				{
					if (this.transformRing == null)
						this.transformRing = this.disruptors.CreateStartupTransformationDisruptor(repository, info, this.complete);

					var next1 = this.transformRing.RingBuffer.Next();
					var claimed1 = this.transformRing.RingBuffer[next1];
					claimed1.AsJournaledMessage(message.Sequence, message.SerializedBody, message.SerializedType, message.SerializedHeaders);
					this.transformRing.RingBuffer.Publish(next1);
				}
			}
		}

		private void Shutdown()
		{
			if (this.transformRing == null)
				return;

			TimeSpan.FromSeconds(1).Sleep();
			this.transformRing.Stop();
		}

		public MessageBootstrapper(IMessageStore store, DisruptorFactory disruptors, Action complete)
		{
			if (store == null) 
				throw new ArgumentNullException("store");
			
			if (disruptors == null) 
				throw new ArgumentNullException("disruptors");
			
			if (complete == null) 
				throw new ArgumentNullException("complete");

			this.store = store;
			this.disruptors = disruptors;
			this.complete = complete;
		}

		protected MessageBootstrapper()
		{
		}

		private readonly IMessageStore store;
		private readonly DisruptorFactory disruptors;
		private readonly Action complete;
		private IDisruptor<TransformationItem> transformRing;
	}
}