namespace Hydrospanner.Wireup
{
	using System;
	using System.Threading;
	using log4net;
	using Persistence;
	using Phases.Journal;
	using Phases.Transformation;

	public class MessageBootstrapper
	{
		public virtual void Restore(BootstrapInfo info, IDisruptor<JournalItem> journalRing, IRepository repository)
		{
			if (info == null)
				throw new ArgumentNullException("info");
			
			if (journalRing == null) 
				throw new ArgumentNullException("journalRing");
			
			if (repository == null) 
				throw new ArgumentNullException("repository");

			this.transformRing = this.disruptors.CreateStartupTransformationDisruptor(
				repository, info, this.snapshotFrequency, () => this.mutex.Set());

			if (this.transformRing != null)
				this.transformRing.Start();

			using (this.transformRing)
			{
				this.Restore(info, journalRing);
				this.mutex.WaitOne();
			}
		}
		private void Restore(BootstrapInfo info, IDisruptor<JournalItem> journalRing)
		{
			var startingSequence = Math.Min(info.DispatchSequence + 1, info.SnapshotSequence + 1);
			Log.InfoFormat("Starting from sequence {0}, will dispatch and will replay messages (this could take some time...).", startingSequence);

			var replayed = false;
			foreach (var message in this.store.Load(startingSequence))
			{
				replayed = this.Replay(info, message) || replayed;
				Dispatch(info, journalRing, message);
			}

			if (!replayed)
				this.mutex.Set();
		}
		private bool Replay(BootstrapInfo info, JournaledMessage message)
		{
			if (message.Sequence <= info.SnapshotSequence)
				return false;

			var next = this.transformRing.RingBuffer.Next();
			var claimed = this.transformRing.RingBuffer[next];
			claimed.AsJournaledMessage(message.Sequence,
				message.SerializedBody,
				message.SerializedType,
				message.SerializedHeaders);
			this.transformRing.RingBuffer.Publish(next);
			return true;
		}
		private static void Dispatch(BootstrapInfo info, IDisruptor<JournalItem> journalRing, JournaledMessage message)
		{
			if (message.Sequence <= info.DispatchSequence)
				return; // already dispatched

			if (message.ForeignId != Guid.Empty)
				return; // only re-dispatch messages which originated here

			var next = journalRing.RingBuffer.Next();
			var claimed = journalRing.RingBuffer[next];
			claimed.AsBootstrappedDispatchMessage(message.Sequence,
				message.SerializedBody,
				message.SerializedType,
				message.SerializedHeaders);
			journalRing.RingBuffer.Publish(next);
		}

		public MessageBootstrapper(IMessageStore store, DisruptorFactory disruptors, int snapshotFrequency)
		{
			if (store == null) 
				throw new ArgumentNullException("store");
			
			if (disruptors == null) 
				throw new ArgumentNullException("disruptors");

			if (snapshotFrequency < 1)
				throw new ArgumentOutOfRangeException("snapshotFrequency");

			this.store = store;
			this.disruptors = disruptors;
			this.snapshotFrequency = snapshotFrequency;
		}

		protected MessageBootstrapper()
		{
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(MessageBootstrapper));
		private readonly AutoResetEvent mutex = new AutoResetEvent(false);
		private readonly IMessageStore store;
		private readonly DisruptorFactory disruptors;
		private readonly int snapshotFrequency;
		private IDisruptor<TransformationItem> transformRing;
	}
}