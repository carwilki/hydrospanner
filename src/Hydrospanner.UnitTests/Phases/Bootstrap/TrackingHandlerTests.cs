#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using Machine.Specifications;

	[Subject(typeof(TrackingHandler))]
	public class when_all_messages_have_been_received
	{
		It should_shutdown_its_own_disruptor;

		static TrackingHandler handler;
		static RingBufferHarness<BootstrapItem> harness;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
