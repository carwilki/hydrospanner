namespace Hydrospanner.Persistence
{
	using System;
	using System.Collections.Generic;
	using Phases.Journal;
	using Wireup;

	public sealed class NullStore : IBootstrapStore, IMessageStore, IDispatchCheckpointStore
	{
		public BootstrapInfo Load()
		{
			return new BootstrapInfo(0, 0, new string[0], new Guid[0]);
		}

		public IEnumerable<JournaledMessage> Load(long startingSequence)
		{
			return new JournaledMessage[0];
		}
		public void Save(List<JournalItem> items)
		{
		}

		public void Save(long sequence)
		{
		}
	}
}