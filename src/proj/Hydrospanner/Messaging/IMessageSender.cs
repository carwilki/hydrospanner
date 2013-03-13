namespace Hydrospanner.Messaging
{
	using System;
	using Hydrospanner.Phases.Journal;

	public interface IMessageSender : IDisposable
	{
		void Send(JournalItem message);
		void Commit();
	}
}