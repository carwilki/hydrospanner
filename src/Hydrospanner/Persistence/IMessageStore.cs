namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Hydrospanner.Phases.Journal;

	public interface IMessageStore
	{
		void Save(List<JournalItem> items);
	}
}