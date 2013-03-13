#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Journal
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Machine.Specifications;
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
				item.AsForeignMessage(SerializedValue, DifferentValue, null, Guid.Empty, null);

			It should_not_alter_the_body = () =>
				item.SerializedBody.SequenceEqual(SerializedValue).ShouldBeTrue();

			const int DifferentValue = 41;
		}

		public class when_the_body_needs_to_be_serialized
		{
			Establish context = () =>
				item.AsTransformationResultMessage(0, Value, null);

			It should_serialize_the_body = () =>
				item.SerializedBody.SequenceEqual(SerializedValue).ShouldBeTrue();

			It should_note_the_serialized_type = () =>
				item.SerializedType.ShouldEqual(Value.GetType().AssemblyQualifiedName);
		}

		public class when_the_headers_have_already_been_serialized
		{
			Establish context = () =>
				item.AsBootstrappedDispatchMessage(0, new byte[0], string.Empty, SerializedValue, Guid.Empty);

			It should_NOT_serialize_the_headers = () =>
				item.SerializedHeaders.SequenceEqual(SerializedValue).ShouldBeTrue();
		}

		public class when_the_headers_need_to_be_serialized
		{
			Establish context = () =>
				item.AsTransformationResultMessage(0, new object(), new Dictionary<string, string> { { "Value", "42" } });

			It should_serialize_the_headers = () =>
				item.SerializedHeaders.SequenceEqual(SerializedValue).ShouldBeTrue();
		}

		Establish context = () =>
		{
			handler = new SerializationHandler(new JsonSerializer());
			item = new JournalItem();
		};

		Because of = () =>
				handler.OnNext(item, 0, false);

		static readonly Dictionary<string, string> Value = new Dictionary<string, string> { { "Value", "42" } };
		static readonly byte[] SerializedValue = Encoding.UTF8.GetBytes("{\r\n  \"Value\": \"42\"\r\n}");
		static SerializationHandler handler;
		static JournalItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
