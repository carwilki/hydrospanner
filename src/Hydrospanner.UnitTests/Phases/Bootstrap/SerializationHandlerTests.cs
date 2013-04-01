#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Machine.Specifications;
	using NSubstitute;
	using Serialization;

	[Subject(typeof(SerializationHandler))]
	public class when_a_null_serializer_is_provided
	{
		It should_throw_an_exception = () =>
			Catch.Exception(() => new SerializationHandler(null)).ShouldBeOfType<ArgumentNullException>();
	}

	public class when_an_item_is_handled
	{
		Establish context = () =>
		{
			item = new BootstrapItem
			{
				SerializedMemento = new byte[] { 0, 1, 2, 3 },
				SerializedType = "some serialized type"
			};

			serializer = Substitute.For<ISerializer>();
			serializer.Deserialize(item.SerializedMemento, item.SerializedType).Returns(42);

			handler = new SerializationHandler(serializer);
		};

		Because of = () =>
			handler.OnNext(item, 0, false);

		It should_deserialize_the_item = () =>
			item.Memento.ShouldEqual(42);

		static BootstrapItem item;
		static ISerializer serializer;
		static SerializationHandler handler;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
