#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Journal
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;

	[Subject(typeof(JournalItem))]
	public class when_initializing_a_foreign_message
	{
		Establish context = () =>
			item = new JournalItem();

		Because of = () =>
			item.AsForeignMessage(serializedBody, Body, headers, ForeignId, Acknowledgement);

		It should_set_the_following_properties_according_to_the_input_arguments = () =>
		{
			item.ItemActions.ShouldEqual(JournalItemAction.Acknowledge | JournalItemAction.Journal);
			item.Acknowledgement.ShouldEqual(Acknowledgement);
			item.SerializedBody.ShouldEqual(serializedBody);
			item.Headers.ShouldEqual(headers);
			item.ForeignId.ShouldEqual(ForeignId);
			item.SerializedType.ShouldStartWith("System.String, mscorlib");
			item.Body.ShouldEqual(Body);
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.MessageSequence.ShouldEqual(0);
			item.SerializedHeaders.ShouldBeNull();
		};

		const string Body = "Body";
		static JournalItem item;
		static readonly byte[] serializedBody = new byte[] { 1, 2, 3 };
		static readonly Dictionary<string, string> headers = new Dictionary<string, string>();
		static readonly Guid ForeignId = Guid.NewGuid();
		static readonly Action Acknowledgement = Console.WriteLine;
	}

	[Subject(typeof(JournalItem))]
	public class when_initializing_a_local_message
	{
		Establish context = () =>
			item = new JournalItem();

		Because of = () =>
			item.AsLocalMessage(42, Body, headers);

		It should_set_the_following_properties_according_to_the_input_arguments = () =>
		{
			item.ItemActions.ShouldEqual(JournalItemAction.Dispatch | JournalItemAction.Journal);
			item.MessageSequence.ShouldEqual(42);
			item.Headers.ShouldEqual(headers);
			item.SerializedType.ShouldStartWith("System.String, mscorlib");
			item.Body.ShouldEqual(Body);
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.SerializedHeaders.ShouldBeNull();
			item.Acknowledgement.ShouldBeNull();
			item.ForeignId.ShouldEqual(Guid.Empty);
			item.SerializedBody.ShouldBeNull();
		};

		const string Body = "Body";
		static readonly Dictionary<string, string> headers = new Dictionary<string, string>();
		static JournalItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
