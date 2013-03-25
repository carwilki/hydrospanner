namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Configuration;
	using Journal;
	using Persistence;

	public class MessageBootstrapper
	{
		public virtual void Restore(BootstrapInfo info, IDisruptor<JournalItem> journalRing, IRepository repository)
		{
			if (info == null) throw new ArgumentNullException("info");
			if (journalRing == null) throw new ArgumentNullException("journalRing");
			if (repository == null) throw new ArgumentNullException("repository");

			// TODO: read all messages from info.DispatchSequence || info.SnapshotSequence (whichever is less)

			// for each message
			// if message.Seq > info.DispatchSequence, push to journal ring for dispatching (it hasn't been dispatched yet)

			// potentially need to create CreateStartupTransformationDisruptor (and start)
			// if message.Seq > info.SnapshotSequence, push to transformation ring

			// once all messages pushed to either dispatch or startup transformation, shut down startup transformation disruptor

			throw new NotImplementedException();
		}

		public MessageBootstrapper(IMessageStore store, DisruptorFactory disruptors)
		{
			if (store == null) throw new ArgumentNullException("store");
			if (disruptors == null) throw new ArgumentNullException("disruptors");

			this.store = store;
			this.disruptors = disruptors;
		}
		protected MessageBootstrapper()
		{
		}

		readonly IMessageStore store;
		readonly DisruptorFactory disruptors;
	}
}