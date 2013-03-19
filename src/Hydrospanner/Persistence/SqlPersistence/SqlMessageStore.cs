namespace Hydrospanner.Persistence.SqlPersistence
{
	using System.Collections.Generic;
	using Hydrospanner.Phases.Journal;

	public class SqlMessageStore : IMessageStore
	{
		public bool Save(List<JournalItem> items)
		{
			return false;
		}
	}
}