namespace Hydrospanner.Messaging
{
	using System;
	using Phases.Journal;

	internal interface IMessageSender : IDisposable
	{
		bool Send(JournalItem message);
		bool Commit();
	}
}