#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Journal
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Machine.Specifications;
	using NSubstitute;
	using Serialization;

	[Subject(typeof(SerializationHandler))]
	public class when_serializing_journal_items
	{
		public class when_the_body_is_null
		{
			Establish context = () =>
				item.AsTransformationResultMessage(0, null, null);

			It should_not_assign_the_serialized_type = () =>
				item.SerializedType.ShouldBeNull();
		}

		public class when_the_body_has_already_been_serialized
		{
			Establish context = () =>
				item.AsForeignMessage(42, SerializedValue, DifferentValue, null, Guid.Empty, null);

			It should_not_alter_the_body = () =>
				item.SerializedBody.SequenceEqual(SerializedValue).ShouldBeTrue();

			const int DifferentValue = 41;
		}

		public class when_the_body_needs_to_be_serialized
		{
			Establish context = () =>
			{
				serializer = Substitute.For<ISerializer>();
				serializer.Serialize(Value).Returns(SerializedValue);
				
				handler = new SerializationHandler(serializer);
				item.AsTransformationResultMessage(0, Value, null);
			};

			It should_serialize_the_body = () =>
				item.SerializedBody.SequenceEqual(SerializedValue).ShouldBeTrue();

			It should_note_the_serialized_type = () =>
				item.SerializedType.ShouldEqual(Value.ResolvableTypeName());

			static ISerializer serializer;
		}

		public class when_the_headers_have_already_been_serialized
		{
			Establish context = () =>
				item.AsBootstrappedDispatchMessage(0, new byte[0], string.Empty, SerializedValue);

			It should_NOT_serialize_the_headers = () =>
				item.SerializedHeaders.SequenceEqual(SerializedValue).ShouldBeTrue();

			It should_deserialize_the_serialized_headers = () =>
				item.Headers.ShouldNotBeNull();
		}

		public class when_the_headers_need_to_be_serialized
		{
			Establish context = () =>
			{
				serializer = Substitute.For<ISerializer>();
				var headers = new Dictionary<string, string> { { "Value", "42" } };
				serializer.Serialize(headers).Returns(SerializedValue);

				handler = new SerializationHandler(serializer);
				item.AsTransformationResultMessage(0, new object(), headers);
			};

			It should_serialize_the_headers = () =>
				item.SerializedHeaders.SequenceEqual(SerializedValue).ShouldBeTrue();

			static ISerializer serializer;
		}

		public class when_the_headers_are_an_empty_collection
		{
			Establish context = () =>
			{
				serializer = Substitute.For<ISerializer>();
				var headers = new Dictionary<string, string>();

				handler = new SerializationHandler(serializer);
				item.AsTransformationResultMessage(0, new object(), headers);
			};

			It should_leave_the_collection_blank = () =>
				item.SerializedHeaders.ShouldBeNull();

			It should_not_attempt_to_serialize_the_empty_header_collection = () =>
				serializer.Received(0).Serialize(Arg.Any<Dictionary<string, string>>());

			static ISerializer serializer;
		}

		Establish context = () =>
		{
			handler = new SerializationHandler(new JsonSerializer());
			item = new JournalItem();
		};

		Because of = () =>
				handler.OnNext(item, 0, false);

		static readonly Dictionary<string, string> Value = new Dictionary<string, string> { { "Value", "42" } };
		static readonly byte[] SerializedValue = Encoding.UTF8.GetBytes("{\n  \"Value\": \"42\"\n}");
		static SerializationHandler handler;
		static JournalItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
