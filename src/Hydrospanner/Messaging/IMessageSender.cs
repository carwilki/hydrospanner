namespace Hydrospanner.Messaging
{
	using System;
	using Phases.Journal;
	using Phases.Snapshot;

	public interface IMessageSender : IDisposable
	{
		bool Send(SnapshotItem message);
		bool Send(JournalItem message);
		bool Commit();
	}
}