namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Hydrospanner.Phases.Journal;

	public interface IMessageStore
	{
		bool Save(List<JournalItem> items);
		IEnumerable<JournalItem> Load(long snapshotSequence, long dispatchSequence);
	}
}