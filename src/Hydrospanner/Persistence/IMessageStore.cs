namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Configuration;
	using Phases.Journal;

	public interface IMessageStore
	{
		void Save(List<JournalItem> items);
		IEnumerable<JournaledMessage> Load(long startingSequence);
	}
}