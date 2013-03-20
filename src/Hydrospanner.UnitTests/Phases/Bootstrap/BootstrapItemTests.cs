#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using Machine.Specifications;

	[Subject(typeof(BootstrapItem))]
	public class when_initializing_the_item_as_a_snapshot
	{
		Establish context = () =>
			item = new BootstrapItem();

		Because of = () =>
			item.AsSnapshot(typeof(string), new byte[] { 1, 2, 3 });

		It should_initialize_the_following_properties = () =>
		{
			item.MementoType.ShouldEqual(typeof(string));
			item.SerializedMemento.ShouldBeLike(new byte[] { 1, 2, 3 });
		};

		It should_initialize_the_following_properties_to_their_default_values = () =>
		{
			item.Body.ShouldBeNull();
			item.Headers.ShouldBeNull();
			item.MessageSequence.ShouldEqual(0);
			item.SerializedBody.ShouldBeNull();
			item.SerializedHeaders.ShouldBeNull();
			item.SerialziedMessageType.ShouldBeNull();
		};

		static BootstrapItem item;
	}

	[Subject(typeof(BootstrapItem))]
	public class when_initializing_the_item_as_a_replay_message
	{
		Establish context = () =>
			item = new BootstrapItem();

		Because of = () =>
			item.AsReplayMessage(42, "type", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 });

		It should_initialize_the_following_properties = () =>
		{
			item.MessageSequence.ShouldEqual(42);
			item.SerializedBody.ShouldBeLike(new byte[] { 1, 2, 3 });
			item.SerializedHeaders.ShouldBeLike(new byte[] { 4, 5, 6 });
			item.SerialziedMessageType.ShouldEqual("type");
		};

		It should_initialize_the_following_properties_to_their_default_values = () =>
		{
			item.MementoType.ShouldBeNull();
			item.SerializedMemento.ShouldBeNull();
			item.Body.ShouldBeNull();
			item.Headers.ShouldBeNull();
		};

		static BootstrapItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
