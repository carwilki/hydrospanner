#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Machine.Specifications;
	using Serialization;

	[Subject(typeof(DeserializationHandler))]
	public class when_deserializing_the_transformation_item
	{
		public class when_the_body_exists
		{
			Establish context = () =>
				item.AsLocalMessage(42, Body, null);

			It should_not_alter_the_body = () =>
				item.Body.ShouldEqual(Body);
		}

		public class when_the_body_is_missing
		{
			Establish context = () =>
				item.AsForeignMessage(Encoding.UTF8.GetBytes(Body), Headers.GetType().AssemblyQualifiedName, null, Guid.Empty, null);

			It should_deserialize_the_body = () =>
				item.Body.ShouldEqual(Headers);
		}

		public class when_the_headers_exist
		{
			Establish context = () =>
				item.AsLocalMessage(42, Body, Headers);

			It should_not_alter_the_headers = () =>
			{
				item.Headers.ShouldEqual(Headers);
				item.Headers.Keys.Count.ShouldEqual(1);
				item.Headers[Key].ShouldEqual(Value);
			};
		}

		public class when_the_headers_are_missing
		{
			Establish context = () =>
				item.AsJournaledMessage(
					42, Encoding.UTF8.GetBytes(Body), Headers.GetType().AssemblyQualifiedName, Encoding.UTF8.GetBytes(Body));

			It should_deserialize_the_headers = () =>
				item.Headers.ShouldBeLike(Headers);
		}

		Establish context = () =>
		{
			handler = new DeserializationHandler(new JsonSerializer());
			item = new TransformationItem();
		};

		Because of = () =>
			handler.OnNext(item, 0, false);

		const string Key = "greeting";
		const string Value = "hi";
		static readonly string Body = "{{ \"{0}\": \"{1}\" }}".FormatWith(Key, Value);
		static readonly Dictionary<string, string> Headers = new Dictionary<string, string> { { Key, Value } };
		static DeserializationHandler handler;
		static TransformationItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
