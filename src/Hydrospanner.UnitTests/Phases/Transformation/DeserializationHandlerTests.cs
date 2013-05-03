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

		public class when_a_foreign_message_throws_a_serialization_exception
		{
			Establish context = () =>
				item.AsForeignMessage(body, Headers.GetType().AssemblyQualifiedName, headers, Guid.Empty, success => received = success);

			It should_provide_false_to_the_callback = () =>
				((bool)received).ShouldBeFalse();

			It should_NOT_throw_an_exception = () =>
				thrown.ShouldBeNull();

			It should_clear_the_message_body = () =>
				item.Body.ShouldBeNull();

			It should_clear_the_message_headers = () =>
				item.Headers.ShouldBeNull();
			
			static readonly Dictionary<string, string> headers = new Dictionary<string, string>(); 
			static readonly byte[] body = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
			static object received;
		}

		public class when_deserializers_run_in_parallel
		{
			Establish context = () =>
			{
				handler = new DeserializationHandler(new JsonSerializer(), 2, 1);
				item.AsJournaledMessage(
					42, Encoding.UTF8.GetBytes(Body), Headers.GetType().AssemblyQualifiedName, Encoding.UTF8.GetBytes(Body));
			};

			It should_skip_items_which_do_not_match_the_mod = () =>
				item.Body.ShouldBeNull();
		}

		Establish context = () =>
		{
			handler = new DeserializationHandler(new JsonSerializer());
			item = new TransformationItem();
		};

		Because of = () =>
			thrown = Catch.Exception(() => handler.OnNext(item, 0, false));

		Cleanup afer = () =>
			thrown = null;

		const string Key = "greeting";
		const string Value = "hi";
		static readonly string Body = "{{ \"{0}\": \"{1}\" }}".FormatWith(Key, Value);
		static readonly Dictionary<string, string> Headers = new Dictionary<string, string> { { Key, Value } };
		static DeserializationHandler handler;
		static TransformationItem item;
		static Exception thrown;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
