namespace Hydrospanner.Persistence
{
	using System;
	using System.Collections.Generic;

	public class BootstrapInfo
	{
		public long JournaledSequence { get; set; }
		public long DispatchSequence { get; set; }
		public long SnapshotSequence { get; set; }
		public IEnumerable<string> SerializedTypes { get; set; }
		public ICollection<Guid> DuplicateIdentifiers { get; set; }

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
			this.JournaledSequence = journal;
			this.DispatchSequence = dispatch;
			this.SnapshotSequence = snapshot;
			this.SerializedTypes = types;
			this.DuplicateIdentifiers = identifiers;
		}
	}
}