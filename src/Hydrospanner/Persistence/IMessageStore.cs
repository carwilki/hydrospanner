namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Phases.Bootstrap;
	using Phases.Journal;

	internal interface IMessageStore
	{
		void Save(List<JournalItem> items);
		IEnumerable<BootstrapJournalMessage> LoadFrom(long sequence);
	}
}