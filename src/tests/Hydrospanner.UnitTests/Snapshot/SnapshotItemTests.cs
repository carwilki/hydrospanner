#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Snapshot
{
	using Machine.Specifications;

	[Subject(typeof(SnapshotItem))]
	public class when_initializing_a_public_snapshot_item
	{
		Establish context = () =>
			item = new SnapshotItem();

		Because of = () =>
			item.AsPublicSnapshot("key", "value");

		It should_set_the_following_properties_according_to_the_given_arguments = () =>
		{
			item.IsPublicSnapshot.ShouldBeTrue();
			item.Key.ShouldEqual("key");
			item.Memento.ShouldEqual("value");
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.CurrentSequence.ShouldEqual(0);
			item.MementosRemaining.ShouldEqual(0);
			item.Serialized.ShouldBeNull();
		};

		static SnapshotItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
