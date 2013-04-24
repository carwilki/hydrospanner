#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Journal
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
			item.AsForeignMessage(42, serializedBody, Body, headers, ForeignId, Acknowledgment);

		It should_set_the_following_properties_according_to_the_input_arguments = () =>
		{
			item.MessageSequence.ShouldEqual(42);
			item.ItemActions.ShouldEqual(JournalItemAction.Acknowledge | JournalItemAction.Journal);
			item.Acknowledgment.ShouldEqual(Acknowledgment);
			item.SerializedBody.ShouldEqual(serializedBody);
			item.Headers.ShouldEqual(headers);
			item.ForeignId.ShouldEqual(ForeignId);
			item.Body.ShouldEqual(Body);
			item.SerializedType.ShouldEqual(Body.ResolvableTypeName());
		};

		It should_set_the_following_properties_to_their_default_values = () =>
			item.SerializedHeaders.ShouldBeNull();

		const string Body = "Body";
		static JournalItem item;
		static readonly byte[] serializedBody = new byte[] { 1, 2, 3 };
		static readonly Dictionary<string, string> headers = new Dictionary<string, string>();
		static readonly Guid ForeignId = Guid.NewGuid();
		static readonly Action<bool> Acknowledgment = success => { };
	}

	[Subject(typeof(JournalItem))]
	public class when_initializing_a_foreign_message_and_assigning_a_zero_sequence
	{
		Establish context = () =>
			item = new JournalItem();

		Because of = () =>
			item.AsForeignMessage(0, serializedBody, Body, headers, ForeignId, Acknowledgment);

		It should_set_the_following_properties_according_to_the_input_arguments = () =>
		{
			item.ItemActions.ShouldEqual(JournalItemAction.Acknowledge);
			item.Acknowledgment.ShouldEqual(Acknowledgment);
			item.SerializedBody.ShouldEqual(serializedBody);
			item.Headers.ShouldEqual(headers);
			item.ForeignId.ShouldEqual(ForeignId);
			item.Body.ShouldEqual(Body);
			item.SerializedType.ShouldEqual(Body.ResolvableTypeName());
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
		static readonly Action<bool> Acknowledgment = success => { };
	}

	[Subject(typeof(JournalItem))]
	public class when_initializing_a_tranformation_message
	{
		Establish context = () =>
			item = new JournalItem();

		Because of = () =>
			item.AsTransformationResultMessage(42, Body, headers);

		It should_set_the_following_properties_according_to_the_input_arguments = () =>
		{
			item.ItemActions.ShouldEqual(JournalItemAction.Dispatch | JournalItemAction.Journal);
			item.MessageSequence.ShouldEqual(42);
			item.Headers.ShouldEqual(headers);
			item.Body.ShouldEqual(Body);
			item.SerializedType.ShouldEqual(Body.ResolvableTypeName());
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.SerializedHeaders.ShouldBeNull();
			item.Acknowledgment.ShouldBeNull();
			item.ForeignId.ShouldEqual(Guid.Empty);
			item.SerializedBody.ShouldBeNull();
		};

		const string Body = "Body";
		static readonly Dictionary<string, string> headers = new Dictionary<string, string>();
		static JournalItem item;
	}

	public class when_initializing_a_boostrapped_for_dispatch_purposes
	{
		Establish context = () =>
			item = new JournalItem();

		Because of = () =>
			item.AsBootstrappedDispatchMessage(42, Body, TypeName, Headers);

		It should_set_the_following_properties_according_to_the_input_arguments = () =>
		{
			item.ItemActions.ShouldEqual(JournalItemAction.Dispatch);
			item.MessageSequence.ShouldEqual(42);
			item.SerializedBody.ShouldEqual(Body);
			item.SerializedType.ShouldEqual(TypeName);
			item.SerializedHeaders.ShouldEqual(Headers);
		};

		It should_set_the_following_properties_to_their_default_values = () =>
		{
			item.Acknowledgment.ShouldBeNull();
			item.Body.ShouldBeNull();
			item.Headers.ShouldBeNull();
			item.ForeignId.ShouldEqual(Guid.Empty);
		};

		static JournalItem item;
		const string TypeName = "SerializedType";
		static readonly byte[] Body = new byte[] { 1, 2, 3 };
		static readonly byte[] Headers = new byte[] { 4, 5, 6 };
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
