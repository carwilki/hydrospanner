namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using Phases.Journal;

	public interface IMessageStore
	{
		void Save(List<JournalItem> items);
	}
}