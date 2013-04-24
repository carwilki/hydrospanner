namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using log4net;
	using Persistence;
	using Phases.Journal;
	using Phases.Transformation;

	internal class MessageBootstrapper
	{
		public virtual bool Restore(BootstrapInfo info, IDisruptor<JournalItem> journalRing, IRepository repository)
		{
			if (info == null)
				throw new ArgumentNullException("info");
			
			if (journalRing == null) 
				throw new ArgumentNullException("journalRing");
			
			if (repository == null) 
				throw new ArgumentNullException("repository");

			this.transformRing = this.disruptors.CreateStartupTransformationDisruptor(repository, info, this.OnComplete);
			if (this.transformRing != null)
				this.transformRing.Start();

			using (this.transformRing)
			{
				this.Restore(info, journalRing);
				this.mutex.WaitOne();
				return this.success;
			}
		}
		private void OnComplete(bool result)
		{
			this.success = result;
			this.mutex.Set();
		}
		private void Restore(BootstrapInfo info, IDisruptor<JournalItem> journalRing)
		{
			var startingSequence = Math.Min(info.DispatchSequence + 1, info.SnapshotSequence + 1);
			Log.InfoFormat("Restoring from sequence {0}, will dispatch and will replay messages (this could take some time...).", startingSequence);

			var replayed = false;
			foreach (var message in this.store.Load(startingSequence))
			{
				replayed = this.Replay(info, message) || replayed;
				this.Dispatch(info, journalRing, message);

				if (message.Sequence % 100000 == 0)
					Log.InfoFormat("Pushed message sequence {0} for replay", message.Sequence);
			}

			Log.Info("All journaled messages restored into transformation disruptor; awaiting transformation completion.");
			if (!replayed)
				this.OnComplete(true);
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
		private void Dispatch(BootstrapInfo info, IDisruptor<JournalItem> journalRing, JournaledMessage message)
		{
			if (message.Sequence <= info.DispatchSequence)
				return; // already dispatched

			if (message.ForeignId != Guid.Empty)
				return; // only re-dispatch messages which originated here

			if (this.internalTypes.Contains(message.SerializedType))
				return; // TODO: get this under test

			var next = journalRing.RingBuffer.Next();
			var claimed = journalRing.RingBuffer[next];
			claimed.AsBootstrappedDispatchMessage(message.Sequence,
				message.SerializedBody,
				message.SerializedType,
				message.SerializedHeaders);
			journalRing.RingBuffer.Publish(next);
		}

		public MessageBootstrapper(IMessageStore store, DisruptorFactory disruptors)
		{
			if (store == null) 
				throw new ArgumentNullException("store");
			
			if (disruptors == null) 
				throw new ArgumentNullException("disruptors");

			this.store = store;
			this.disruptors = disruptors;

			var types = this.GetType().Assembly.GetTypes();
			foreach (var type in types)
				if (typeof(IInternalMessage).IsAssignableFrom(type))
					this.internalTypes.Add(type.ResolvableTypeName());
		}
		protected MessageBootstrapper()
		{
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(MessageBootstrapper));
		private readonly HashSet<string> internalTypes = new HashSet<string>();
		private readonly AutoResetEvent mutex = new AutoResetEvent(false);
		private readonly IMessageStore store;
		private readonly DisruptorFactory disruptors;
		private IDisruptor<TransformationItem> transformRing;
		private bool success;
	}
}