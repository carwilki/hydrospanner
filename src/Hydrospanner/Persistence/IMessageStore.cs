namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Hydrospanner.Phases.Bootstrap;
	using Phases.Journal;

	public interface IMessageStore
	{
		void Save(List<JournalItem> items);
		IEnumerable<JournaledMessage> Load(long startingSequence);
	}
}