namespace Hydrospanner.Persistence
{
	using System;
	using System.Collections.Generic;

	public class BootstrapInfo
	{
		public bool Populated { get; private set; }
		public long JournaledSequence { get; private set; }
		public long DispatchSequence { get; private set; }
		public long SnapshotSequence { get; private set; }
		public IEnumerable<string> SerializedTypes { get; private set; }
		public ICollection<Guid> DuplicateIdentifiers { get; private set; }

		public BootstrapInfo AddSnapshotSequence(long snapshot)
		{
			return new BootstrapInfo(this.JournaledSequence, this.DispatchSequence, snapshot, this.SerializedTypes, this.DuplicateIdentifiers);
		}

		public BootstrapInfo(long journal, long dispatch, IEnumerable<string> types, ICollection<Guid> identifiers)
			: this(journal, dispatch, 0, types, identifiers)
		{
		}
		public BootstrapInfo()
		{
		}
		private BootstrapInfo(long journal, long dispatch, long snapshot, IEnumerable<string> types, ICollection<Guid> identifiers) : this()
		{
			this.Populated = true;
			this.JournaledSequence = journal;
			this.DispatchSequence = dispatch;
			this.SnapshotSequence = snapshot;
			this.SerializedTypes = types;
			this.DuplicateIdentifiers = identifiers;
		}
	}
}