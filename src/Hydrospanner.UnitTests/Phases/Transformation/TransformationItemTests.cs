#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;
	using Serialization;

	[Subject(typeof(TransformationItem))]
	public class when_initializing_a_foreign_message
	{
		Establish context = () =>
		{
			item = new TransformationItem();
			body = new byte[] { 1, 2, 3 };
			type = "Type";
			headers = new Dictionary<string, string>();
			foreignId = Guid.NewGuid();
			ack = x => { };
		};

		Because of = () =>
			item.AsForeignMessage(body, type, headers, foreignId, ack);

		It should_set_the_following_properties_according_to_the_given_arguments = () =>
		{
			item.SerializedBody.ShouldEqual(body);
			item.SerializedType.ShouldEqual(type);
			item.Headers.ShouldEqual(headers);
			item.Acknowledgment.ShouldEqual(ack);
		};

		It should_set_the_following_properties_to_their_defaults = () =>
		{
			item.MessageSequence.ShouldEqual(0);
			item.SerializedHeaders.ShouldBeNull();
			item.Body.ShouldBeNull();
			item.IsTransient.ShouldBeFalse();
		};

		static TransformationItem item;
		static byte[] body;
		static string type;
		static Dictionary<string, string> headers;
		static Guid foreignId;
		static Action<bool> ack;
	}

	[Subject(typeof(TransformationItem))]
	public class when_initializing_a_foreign_message_as_transient
	{
		Establish context = () =>
		{
			item = new TransformationItem();
			body = new byte[] { 1, 2, 3 };
			type = "Type";
			headers = new Dictionary<string, string>();
			foreignId = Guid.NewGuid();
			ack = x => { };
		};

		Because of = () =>
			item.AsTransientMessage(body, type, headers, foreignId, ack);

		It should_set_the_following_properties_according_to_the_given_arguments = () =>
		{
			item.SerializedBody.ShouldEqual(body);
			item.SerializedType.ShouldEqual(type);
			item.Headers.ShouldEqual(headers);
			item.Acknowledgment.ShouldEqual(ack);
			item.IsTransient.ShouldBeTrue();
		};

		It should_set_the_following_properties_to_their_defaults = () =>
		{
			item.MessageSequence.ShouldEqual(0);
			item.SerializedHeaders.ShouldBeNull();
			item.Body.ShouldBeNull();
		};

		static TransformationItem item;
		static byte[] body;
		static string type;
		static Dictionary<string, string> headers;
		static Guid foreignId;
		static Action<bool> ack;
	}

	[Subject(typeof(TransformationItem))]
	public class when_initializing_a_local_message
	{
		Establish context = () =>
		{
			item = new TransformationItem();
			sequence = 42;
			body = "hi";
			headers = new Dictionary<string, string>();
		};

		Because of = () =>
			item.AsLocalMessage(sequence, body, headers);

		It should_set_the_following_properties_according_to_the_provided_arguments = () =>
		{
			item.MessageSequence.ShouldEqual(sequence);
			item.SerializedType.ShouldStartWith("System.String, mscorlib");
			item.Body.ShouldEqual(body);
			item.Headers.ShouldEqual(headers);
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.SerializedBody.ShouldBeNull();
			item.SerializedHeaders.ShouldBeNull();
			item.ForeignId.ShouldEqual(Guid.Empty);
			item.Acknowledgment.ShouldBeNull();
			item.IsTransient.ShouldBeFalse();
		};

		static TransformationItem item;
		static long sequence;
		static object body;
		static Dictionary<string, string> headers;
	}

	[Subject(typeof(TransformationItem))]
	public class when_initializing_a_journaled_message
	{
		Establish context = () =>
		{
			item = new TransformationItem();
			sequence = 42;
			body = new byte[] { 1, 2, 3 };
			headers = new byte[] { 4, 5, 6 };
			type = "System.String, mscorlib";
		};

		Because of = () =>
			item.AsJournaledMessage(sequence, body, type, headers);

		It should_set_the_following_properties_according_to_the_provided_arguments = () =>
		{
			item.MessageSequence.ShouldEqual(sequence);
			item.SerializedType.ShouldStartWith("System.String, mscorlib");
			item.SerializedBody.ShouldEqual(body);
			item.SerializedHeaders.ShouldEqual(headers);
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.Body.ShouldBeNull();
			item.Headers.ShouldBeNull();
			item.Acknowledgment.ShouldBeNull();
			item.ForeignId.ShouldEqual(Guid.Empty);
			item.IsTransient.ShouldBeFalse();
		};

		static TransformationItem item;
		static long sequence;
		static byte[] body;
		static byte[] headers;
		static string type;
	}

	[Subject(typeof(TransformationItem))]
	public class when_initializing_a_transient_message
	{
		Establish context = () =>
		{
			item = new TransformationItem();
			body = new object();
		};

		Because of = () =>
			item.AsTransientMessage(body);

		It should_set_the_following_properties_according_to_the_provided_arguments = () =>
		{
			item.Body.ShouldEqual(body);
			item.IsTransient.ShouldBeTrue();
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.Headers.ShouldBeNull();
			item.Acknowledgment.ShouldBeNull();
			item.ForeignId.ShouldEqual(Guid.Empty);
			item.Headers.ShouldBeNull();
			item.MessageSequence.ShouldEqual(0);
			item.SerializedBody.ShouldBeNull();
			item.SerializedHeaders.ShouldBeNull();
			item.SerializedType.ShouldBeNull();
		};

		static TransformationItem item;
		static object body;
	}

	public class when_deserializing_journaled_item_fails
	{
		Establish context = () =>
		{
			item = new TransformationItem();
			item.AsJournaledMessage(1, new byte[] { 1, 2, 3 }, string.Empty, null);
		};

		Because of = () =>
			thrown = Catch.Exception(() => item.Deserialize(new JsonSerializer()));

		It should_clear_any_item_body = () =>
			item.Body.ShouldBeNull();

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();

		static TransformationItem item;
		static Exception thrown;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
