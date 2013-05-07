#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using Machine.Specifications;

	[Subject(typeof(SnapshotItem))]
	public class when_initializing_a_public_snapshot_item
	{
		Establish context = () =>
			item = new SnapshotItem();

		Because of = () =>
			item.AsPublicSnapshot("key", "value", typeof(string), 42);

		It should_set_the_following_properties_according_to_the_given_arguments = () =>
		{
			item.IsPublicSnapshot.ShouldBeTrue();
			item.Key.ShouldEqual("key");
			item.Memento.ShouldEqual("value");
			item.MementoType.ShouldEqual(typeof(string).ResolvableTypeName());
			item.CurrentSequence.ShouldEqual(42);
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.MementosRemaining.ShouldEqual(0);
			item.Serialized.ShouldBeNull();
		};

		static SnapshotItem item;
	}

	[Subject(typeof(SnapshotItem))]
	public class when_initializing_a_part_of_a_system_snapshot
	{
		Establish context = () =>
			item = new SnapshotItem();

		Because of = () =>
			item.AsPartOfSystemSnapshot(1, 2, "value");

		It should_set_the_following_properties_according_to_the_given_arguments = () =>
		{
			item.CurrentSequence.ShouldEqual(1);
			item.MementosRemaining.ShouldEqual(2);
			item.Memento.ShouldEqual("value");
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.IsPublicSnapshot.ShouldBeFalse();
			item.Serialized.ShouldBeNull();
			item.Key.ShouldBeNull();
		};

		static SnapshotItem item;
	}

	public class when_providing_a_cloneable_system_memento
	{
		Establish context = () =>
			item = new SnapshotItem();

		Because of = () =>
			item.AsPartOfSystemSnapshot(42, 0, new Cloner());

		It should_clone_the_memento = () =>
			item.Memento.ShouldEqual("cloned");

		static SnapshotItem item;
	}

	public class when_providing_a_cloneable_public_memento
	{
		Establish context = () =>
			item = new SnapshotItem();

		Because of = () =>
			item.AsPublicSnapshot("key", new Cloner(), typeof(object), 42);

		It should_clone_the_memento = () =>
			item.Memento.ShouldEqual("cloned");

		It should_prefer_the_cloned_memento_type_when_possbile = () =>
			item.MementoType.ShouldEqual(typeof(string).ResolvableTypeName());

		static SnapshotItem item;
	}

	internal class Cloner : ICloneable
	{
		public object Clone()
		{
			return "cloned";
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
