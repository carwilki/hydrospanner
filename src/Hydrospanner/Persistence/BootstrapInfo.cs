namespace Hydrospanner.Persistence
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	public struct BootstrapInfo
	{
		public bool Populated { get; private set; }
		public long JournaledSequence { get; private set; }
		public long DispatchSequence { get; private set; }
		public long SnapshotSequence { get; private set; }
		public IEnumerable<string> SerializedTypes { get; private set; }

		public BootstrapInfo AddSnapshotSequence(long snapshot)
		{
			return new BootstrapInfo(this.JournaledSequence, this.DispatchSequence, snapshot, this.SerializedTypes);
		}

		public BootstrapInfo(long journal, long dispatch, IEnumerable<string> types)
			: this(journal, dispatch, 0, new ReadOnlyCollection<string>(new List<string>(types)))
		{
		}
		private BootstrapInfo(long journal, long dispatch, long snapshot, IEnumerable<string> types) : this()
		{
			this.Populated = true;
			this.JournaledSequence = journal;
			this.DispatchSequence = dispatch;
			this.SnapshotSequence = snapshot;
			this.SerializedTypes = types;
		}
	}
}