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

		static BootstrapItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
