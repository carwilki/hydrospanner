#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using System.Runtime.Serialization;
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

			Type @out;
			serializer = Substitute.For<ISerializer>();
			serializer.Deserialize(item.SerializedMemento, item.SerializedType, out @out).Returns(x =>
			{
				x[2] = DeserializedType;
				return 42;
			});

			handler = new SerializationHandler(serializer);
		};

		Because of = () =>
			handler.OnNext(item, 0, false);

		It should_deserialize_the_item = () =>
			item.Memento.ShouldEqual(42);

		It should_append_the_deserialized_type = () =>
			item.MementoType.ShouldEqual(DeserializedType);

		It should_clear_the_serialized_bytes = () =>
			item.SerializedMemento.ShouldBeNull();

		static readonly Type DeserializedType = typeof(Guid);
		static BootstrapItem item;
		static ISerializer serializer;
		static SerializationHandler handler;
	}
	
	public class when_deserializing_an_item_fails
	{
		Establish context = () =>
		{
			item = new BootstrapItem
			{
				SerializedMemento = new byte[] { 0, 1, 2, 3 },
				SerializedType = "some serialized type",
				Memento = new object() // ensure this gets nullified
			};

			serializer = Substitute.For<ISerializer>();
			serializer.Deserialize(item.SerializedMemento, item.SerializedType).Returns(x => { throw new SerializationException(); });

			handler = new SerializationHandler(serializer);
		};

		Because of = () =>
			thrown = Catch.Exception(() => handler.OnNext(item, 0, false));

		It should_NOT_throw_an_exception = () =>
			thrown.ShouldBeNull();

		It should_nullify_the_body_of_the_item = () =>
			item.Memento.ShouldBeNull();

		static BootstrapItem item;
		static ISerializer serializer;
		static SerializationHandler handler;
		static Exception thrown;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
