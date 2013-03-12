#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Transformation
{
	using Machine.Specifications;

	[Subject(typeof(SerializationHandler))]
	public class when_SerializationHandlerTests
	{
		public class when_the_body_exists
		{
			Establish context = () =>
			{
			};

			It should_not_alter_the_body;
		}

		public class when_the_body_is_missing
		{
			Establish context = () =>
			{
			};

			It should_deserialize_the_body;
		}

		public class when_the_headers_exist
		{
			It should_not_alter_the_headers;
		}

		public class when_the_headers_are_missing
		{
			It should_deserialize_the_headers;
		}

		Establish context = () =>
		{
			handler = new SerializationHandler();
			item = new TransformationItem();
		};

		static SerializationHandler handler;
		static TransformationItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
