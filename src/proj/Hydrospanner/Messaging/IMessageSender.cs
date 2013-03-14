namespace Hydrospanner.Messaging
{
	using System;
	using Hydrospanner.Phases.Journal;

	public interface IMessageSender : IDisposable
	{
		bool Send(JournalItem message);
		bool Commit();
	}
}