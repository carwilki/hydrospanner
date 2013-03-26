namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Phases.Journal;
	using Wireup;

	public interface IMessageStore
	{
		void Save(List<JournalItem> items);
		IEnumerable<JournaledMessage> Load(long startingSequence);
	}
}