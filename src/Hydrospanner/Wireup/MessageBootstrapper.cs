namespace Hydrospanner.Wireup
{
	using System;
	using System.Threading;
	using Persistence;
	using Phases;
	using Phases.Journal;
	using Phases.Transformation;

	public class MessageBootstrapper
	{
		public virtual void Restore(BootstrapInfo info, IDisruptor<JournalItem> journalRing, IRepository repository)
		{
			ValidateInput(info, journalRing, repository);

			using (this.transformRing = this.disruptors.CreateStartupTransformationDisruptor(repository, info, () => this.mutex.Set()))
			{
				this.RestoreFrom(info, journalRing);
				this.mutex.WaitOne();
			}
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

		private void RestoreFrom(BootstrapInfo info, IDisruptor<JournalItem> journalRing)
		{
			var transformed = 0;

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
					transformed++;
					var next = this.transformRing.RingBuffer.Next();
					var claimed = this.transformRing.RingBuffer[next];
					claimed.AsJournaledMessage(message.Sequence, message.SerializedBody, message.SerializedType, message.SerializedHeaders);
					this.transformRing.RingBuffer.Publish(next);
				}
			}

			if (transformed == 0)
				this.mutex.Set();
		}

		public MessageBootstrapper(IMessageStore store, DisruptorFactory disruptors)
		{
			if (store == null) 
				throw new ArgumentNullException("store");
			
			if (disruptors == null) 
				throw new ArgumentNullException("disruptors");

			this.store = store;
			this.disruptors = disruptors;
		}

		protected MessageBootstrapper()
		{
		}

		private readonly AutoResetEvent mutex = new AutoResetEvent(false);
		private readonly IMessageStore store;
		private readonly DisruptorFactory disruptors;
		private IDisruptor<TransformationItem> transformRing;
	}
}