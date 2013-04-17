#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Linq;
	using System.Text;
	using Journal;
	using Machine.Specifications;
	using Serialization;

	[Subject(typeof(DuplicateHandler))]
	public class when_forwarding_messages
	{
		public class and_the_message_was_generated_locally
		{
			Establish context = () =>
			{
				item.AsLocalMessage(42, new object(), null);
				ring = new RingBufferHarness<JournalItem>();
				handler = new DuplicateHandler(store, ring);
			};

			Because of = () =>
				forwarded = handler.Forward(item);

			It should_NOT_forward_the_message = () =>
			{
				forwarded.ShouldBeFalse();
				ring.AllItems.ShouldBeEmpty();
			};
			
			static DuplicateHandler handler;
			static RingBufferHarness<JournalItem> ring;
		}

		public class and_the_message_is_foreign
		{
			public class and_the_message_is_a_duplicate_message
			{
				Establish context = () =>
				{
					store.Contains(ForeignId);
					item.AsForeignMessage(Body, TypeName, null, ForeignId, Console.WriteLine);
					item.Deserialize(new JsonSerializer());

					expected = new JournalItem();
					expected.AsForeignMessage(0, Body, 1, null, ForeignId, Console.WriteLine);
					
					ring = new RingBufferHarness<JournalItem>();
					handler = new DuplicateHandler(store, ring);
				};

				Because of = () =>
					result = handler.Forward(item);

				It should_forward_the_message_for_acknowledgement_only = () =>
				{
					result.ShouldBeTrue();
					actual = ring.AllItems.Single();
					actual.Acknowledgment.ShouldEqual(expected.Acknowledgment);
					actual.Body.ShouldEqual(1);
					actual.ForeignId.ShouldEqual(ForeignId);
					actual.Headers.ShouldBeNull();
					actual.ItemActions.ShouldEqual(JournalItemAction.Acknowledge);
					actual.MessageSequence.ShouldEqual(0);
					actual.SerializedBody.ShouldBeLike(Body);
					actual.SerializedHeaders.ShouldBeNull();
					actual.SerializedType.ShouldEqual(TypeName);
				};

				static bool result;
				static readonly string TypeName = default(int).ResolvableTypeName();
				static readonly Guid ForeignId = Guid.NewGuid();
				static readonly byte[] Body = Encoding.UTF8.GetBytes("1");
				static JournalItem expected;
				static RingBufferHarness<JournalItem> ring;
				static DuplicateHandler handler;
				static JournalItem actual;
			}

			public class and_the_message_is_new
			{
				Establish context = () =>
				{
					item.AsForeignMessage(Body, TypeName, null, ForeignId, Console.WriteLine);
					item.Deserialize(new JsonSerializer());

					ring = new RingBufferHarness<JournalItem>();
					handler = new DuplicateHandler(store, ring);
				};

				Because of = () =>
					result = handler.Forward(item);

				It should_NOT_forward_the_message = () =>
				{
					result.ShouldBeFalse();
					ring.AllItems.ShouldBeEmpty();
				};

				static bool result;
				static readonly string TypeName = typeof(int).AssemblyQualifiedName;
				static readonly Guid ForeignId = Guid.NewGuid();
				static readonly byte[] Body = Encoding.UTF8.GetBytes("1");
				static JournalItem expected;
				static RingBufferHarness<JournalItem> ring;
				static DuplicateHandler handler;
				static JournalItem actual;
			}
		}

		Establish context = () =>
		{
			item = new TransformationItem();
			store = new DuplicateStore(1024);
		};

		static bool forwarded;
		static TransformationItem item;
		static DuplicateStore store;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
