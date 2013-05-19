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
		public virtual bool Restore(BootstrapInfo info, IDisruptor<JournalItem> journalDisruptor, IRepository repository)
		{
			if (info == null)
				throw new ArgumentNullException("info");
			
			if (journalDisruptor == null) 
				throw new ArgumentNullException("journalDisruptor");
			
			if (repository == null) 
				throw new ArgumentNullException("repository");

			using (var disruptor = this.disruptors.CreateStartupTransformationDisruptor(repository, info, this.OnComplete))
			{
				if (disruptor != null)
					disruptor.Start();

				var ring = disruptor == null ? null : disruptor.RingBuffer;
				this.Restore(info, ring, journalDisruptor.RingBuffer);
				this.mutex.WaitOne();
				return this.success;
			}
		}
		private void OnComplete(bool result)
		{
			this.success = result;
			this.mutex.Set();
		}
		private void Restore(BootstrapInfo info, IRingBuffer<TransformationItem> transformRing, IRingBuffer<JournalItem> journalRing)
		{
			var startingSequence = Math.Min(info.DispatchSequence + 1, info.SnapshotSequence + 1);
			Log.InfoFormat("Restoring from sequence {0}, will dispatch and will replay messages (this could take some time...).", startingSequence);

			var replayed = false;
			foreach (var message in this.store.Load(startingSequence))
			{
				replayed = Replay(info, transformRing, message) || replayed;
				this.Dispatch(info, journalRing, message);

				if (message.Sequence % 25000 == 0)
					Log.InfoFormat("Pushed message sequence {0} for replay", message.Sequence);
			}

			Log.Info("All journaled messages restored into transformation disruptor; awaiting transformation completion.");
			if (!replayed)
				this.OnComplete(true);
		}
		private static bool Replay(BootstrapInfo info, IRingBuffer<TransformationItem> transformRing, JournaledMessage message)
		{
			if (message.Sequence <= info.SnapshotSequence)
				return false;

			var next = transformRing.Next();
			var claimed = transformRing[next];
			claimed.AsJournaledMessage(message.Sequence,
				message.SerializedBody,
				message.SerializedType,
				message.SerializedHeaders);
			transformRing.Publish(next);
			return true;
		}
		private void Dispatch(BootstrapInfo info, IRingBuffer<JournalItem> journalRing, JournaledMessage message)
		{
			if (message.Sequence <= info.DispatchSequence)
				return; // already dispatched

			if (message.ForeignId != Guid.Empty)
				return; // only re-dispatch messages which originated here

			if (this.internalTypes.Contains(message.SerializedType))
				return;

			var next = journalRing.Next();
			var claimed = journalRing[next];
			claimed.AsBootstrappedDispatchMessage(message.Sequence,
				message.SerializedBody,
				message.SerializedType,
				message.SerializedHeaders);
			journalRing.Publish(next);
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
			for (var i = 0; i < types.Length; i++)
				if (typeof(IInternalMessage).IsAssignableFrom(types[i]))
					this.internalTypes.Add(types[i].ResolvableTypeName());
		}
		protected MessageBootstrapper()
		{
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(MessageBootstrapper));
		private readonly HashSet<string> internalTypes = new HashSet<string>();
		private readonly AutoResetEvent mutex = new AutoResetEvent(false);
		private readonly IMessageStore store;
		private readonly DisruptorFactory disruptors;
		private bool success;
	}
}