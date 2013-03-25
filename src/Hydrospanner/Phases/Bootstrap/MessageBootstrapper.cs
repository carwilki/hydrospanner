namespace Hydrospanner.Phases.Bootstrap
{
	using Hydrospanner.Persistence;

	public class MessageBootstrapper
	{
		public virtual void Restore(BootstrapInfo info, object snapshotRing, object journalRing, IRepository repository)
		{
			// TODO: standard null ref checks/throw

			// TODO: read all messages from info.DispatchSequence || info.SnapshotSequence (whichever is less)

			// for each message
			// if message.Seq > info.DispatchSequence, push to dispatch ring (it hasn't been dispatched yet)

			// potentially need to create CreateStartupTransformationDisruptor (and start)
			// if message.Seq > info.SnapshotSequence, push to transformation ring

			// once all messages pushed to either dispatch or startup transformation, shut down startup transformation disruptor

			throw new System.NotImplementedException();
		}
	}
}