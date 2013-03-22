namespace Hydrospanner.Configuration
{
	using Hydrospanner.Phases;
	using Hydrospanner.Phases.Bootstrap;
	using Hydrospanner.Phases.Journal;
	using Hydrospanner.Phases.Snapshot;
	using Hydrospanner.Phases.Transformation;

	public class DisruptorFactory
	{
		public virtual IDisruptor<BootstrapItem> CreateBootstrapDisruptor(/* deps */)
		{
			return null;
		}
		public virtual IDisruptor<TransformationItem> CreateStartupTransformationDisruptor(/* deps */)
		{
			return null;
		}

		public virtual IDisruptor<JournalItem> CreateJournalDisruptor(/* deps */)
		{
			return null;
		}
		public virtual IDisruptor<SnapshotItem> CreateSnapshotDisruptor(/* deps */)
		{
			return null;
		}
		public virtual IDisruptor<TransformationItem> CreateTransformationDisruptor(/* deps */)
		{
			return null;
		}
	}
}