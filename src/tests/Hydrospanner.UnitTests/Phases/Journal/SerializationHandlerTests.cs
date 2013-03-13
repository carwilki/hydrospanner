#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Journal
{
	using Hydrospanner.Serialization;
	using Machine.Specifications;

	[Subject(typeof(SerializationHandler))]
	public class when_serializing_journal_items
	{
		public class when_the_body_has_already_been_serialized
		{
		}

		public class when_the_body_needs_to_be_serialized
		{
		}

		public class when_the_headers_have_already_been_serialized
		{
		}

		public class when_the_headers_need_to_be_serialized
		{
		}

		Establish context = () =>
		{
			handler = new SerializationHandler(new JsonSerializer());
			item = new JournalItem();
		};

		static SerializationHandler handler;
		static JournalItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
