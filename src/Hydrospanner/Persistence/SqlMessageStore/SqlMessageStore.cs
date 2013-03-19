namespace Hydrospanner.Persistence.SqlMessageStore
{
	using System.Collections.Generic;
	using Hydrospanner.Phases.Journal;

	public class SqlMessageStore : IMessageStore
	{
		public bool Save(List<JournalItem> items)
		{
			return false;
		}
		public IEnumerable<JournalItem> Load(long snapshotSequence, long dispatchSequence)
		{
			return null;
		}
	}
}