namespace Hydrospanner.Wireup
{
	using System;
	using System.Threading;
	using Persistence;
	using Phases.Journal;
	using Phases.Transformation;
	using log4net;

	public class MessageBootstrapper
	{
		public virtual void Restore(BootstrapInfo info, IDisruptor<JournalItem> journalRing, IRepository repository)
		{
			ValidateInput(info, journalRing, repository);

			this.transformRing = this.disruptors.CreateStartupTransformationDisruptor(
				repository, info, this.snapshotFrequency, () => this.mutex.Set());

			if (this.transformRing != null)
				this.transformRing.Start();

			using (this.transformRing)
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

			var loadPoint = LogOperations(info);

			foreach (var message in this.store.Load(loadPoint))
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

		static long LogOperations(BootstrapInfo info)
		{
			var loadPoint = Math.Min(info.DispatchSequence + 1, info.SnapshotSequence + 1);
			var toDispatch = Math.Max(0, info.DispatchSequence - loadPoint);
			var toReplay = Math.Max(0, info.JournaledSequence - loadPoint);

			Log.InfoFormat(
			    "Starting from sequence {0}, will dispatch {1} messages and will replay {2} messages (this could take some time...).",
			    loadPoint,
			    toDispatch,
			    toReplay);

			return loadPoint;
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