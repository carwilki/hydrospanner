#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Configuration;
	using Machine.Specifications;
	using NSubstitute;

	[Subject(typeof(SnapshotBootstrapper))]
	public class when_bootstrapping_from_a_system_snapshot
	{
		public class when_the_snapshot_factory_provided_is_null
		{
			It should_throw_an_exception = () =>
				Catch.Exception(() => new SnapshotBootstrapper(null, Substitute.For<DisruptorFactory>())).ShouldBeOfType<ArgumentNullException>();
		}
		public class when_the_disruptor_factory_provided_is_null
		{
			It should_throw_an_exception = () =>
				Catch.Exception(() => new SnapshotBootstrapper(Substitute.For<SnapshotFactory>(), null)).ShouldBeOfType<ArgumentNullException>();
		}

		public class when_reading_streaming_in_a_system_snapshot
		{
			It should_load_from_the_journaled_sequence;
			It should_create_a_disruptor;
			It should_start_the_disruptor;
			It should_publish_each_item_to_the_journal;
			It should_shutdown_the_disruptor;
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
