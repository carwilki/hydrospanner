namespace Hydrospanner.Messaging
{
	using System;
	using Phases.Journal;

	public interface IMessageSender : IDisposable
	{
		bool Send(JournalItem message);
		bool Commit();
	}
}