namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Hydrospanner.Phases.Journal;

	public interface IMessageStorage
	{
		bool Save(List<JournalItem> items);
	}
}