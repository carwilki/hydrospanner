#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Snapshot
{
	using System.Text;
	using Machine.Specifications;
	using Serialization;

	[Subject(typeof(SerializationHandler))]
	public class when_serializing_the_snapshot
	{
		Establish context = () =>
		{
			handler = new SerializationHandler(new JsonSerializer());
			item = new SnapshotItem();
			memento = new Memento()
			{
				First = "1"
			};
			item.AsPublicSnapshot("Key", memento, 42);
		};

		Because of = () =>
			handler.OnNext(item, 0, false);

		It should_serialize_the_memento = () =>
		{
			var json = Encoding.UTF8.GetString(item.Serialized)
				.Replace("\r\n", string.Empty)
				.Replace(" ", string.Empty);

			json.ShouldEqual("{\"first\":\"1\"}");
		};

		static Memento memento;
		static SerializationHandler handler;
		static SnapshotItem item;

		private class Memento
		{
			public string First { get; set; }
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
