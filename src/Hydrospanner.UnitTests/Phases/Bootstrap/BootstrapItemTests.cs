﻿#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using Machine.Specifications;

	[Subject(typeof(BootstrapItem))]
	public class when_initializing_the_item_as_a_snapshot
	{
		Establish context = () =>
			item = new BootstrapItem { Memento = new object() };

		Because of = () =>
			item.AsSnapshot("MyKey", typeof(string).AssemblyQualifiedName, new byte[] { 1, 2, 3 });

		It should_initialize_the_following_properties = () =>
		{
			item.Key.ShouldEqual("MyKey");
			item.SerializedType.ShouldEqual(typeof(string).AssemblyQualifiedName);
			item.SerializedMemento.ShouldBeLike(new byte[] { 1, 2, 3 });
			item.Memento.ShouldBeNull();
		};

		static BootstrapItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
