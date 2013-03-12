#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Transformation
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;

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
			ack = Console.WriteLine;
		};

		Because of = () =>
			item.AsForeignMessage(body, type, headers, foreignId, ack);

		It should_set_the_following_properties_according_to_the_given_arguments = () =>
		{
			item.SerializedBody.ShouldEqual(body);
			item.SerializedType.ShouldEqual(type);
			item.Headers.ShouldEqual(headers);
			item.Acknowledgement.ShouldEqual(ack);
			item.CanJournal.ShouldBeTrue();
		};

		It should_set_the_following_properties_to_their_defaults = () =>
		{
			item.MessageSequence.ShouldEqual(0);
			item.SerializedHeaders.ShouldBeNull();
			item.Body.ShouldBeNull();
			item.IsDocumented.ShouldBeFalse();
			item.IsLocal.ShouldBeFalse();
			item.IsDuplicate.ShouldBeFalse();
		};

		static TransformationItem item;
		static byte[] body;
		static string type;
		static Dictionary<string, string> headers;
		static Guid foreignId;
		static Action ack;
	}

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
			item.IsLocal.ShouldBeTrue();
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.SerializedBody.ShouldBeNull();
			item.SerializedHeaders.ShouldBeNull();
			item.CanJournal.ShouldBeFalse();
			item.IsDocumented.ShouldBeFalse();
			item.IsDuplicate.ShouldBeFalse();
			item.ForeignId.ShouldEqual(Guid.Empty);
			item.Acknowledgement.ShouldBeNull();
		};

		static TransformationItem item;
		static long sequence;
		static object body;
		static Dictionary<string, string> headers;
	}

	public class when_initializing_a_journaled_message
	{
		Establish context = () =>
		{
			item = new TransformationItem();
			sequence = 42;
			body = new byte[] { 1, 2, 3 };
			headers = new byte[] { 4, 5, 6 };
			type = "System.String, mscorlib";
			foreignId = Guid.NewGuid();
		};

		Because of = () =>
			item.AsJournaledMessage(sequence, body, type, headers, foreignId);

		It should_set_the_following_properties_according_to_the_provided_arguments = () =>
		{
			item.MessageSequence.ShouldEqual(sequence);
			item.SerializedType.ShouldStartWith("System.String, mscorlib");
			item.SerializedBody.ShouldEqual(body);
			item.SerializedHeaders.ShouldEqual(headers);
			item.IsLocal.ShouldBeTrue();
			item.IsDocumented.ShouldBeTrue();
			item.ForeignId.ShouldEqual(foreignId);
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.Body.ShouldBeNull();;
			item.Headers.ShouldBeNull();
			item.CanJournal.ShouldBeFalse();
			item.IsDuplicate.ShouldBeFalse();
			item.Acknowledgement.ShouldBeNull();
		};

		static TransformationItem item;
		static long sequence;
		static byte[] body;
		static byte[] headers;
		static string type;
		static Guid foreignId;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
