#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Snapshot
{
	using System.Collections.Generic;
	using System.Text;
	using Machine.Specifications;

	[Subject(typeof(SerializationHandler))]
	public class when_serializing_the_snapshot
	{
		Establish context = () =>
		{
			handler = new SerializationHandler(new JsonSerializer());
			item = new SnapshotItem();
			memento = new Dictionary<string, string> { { "Hello", "World" } };
			item.AsPublicSnapshot("Key", memento);
		};

		Because of = () =>
			handler.OnNext(item, 0, false);

		It should_serialize_the_memento = () =>
		{
			var json = Encoding.UTF8.GetString(item.Serialized)
				.Replace("\r\n", string.Empty)
				.Replace(" ", string.Empty);

			json.ShouldEqual("{\"Hello\":\"World\"}");
		};

		static Dictionary<string, string> memento;
		static SerializationHandler handler;
		static SnapshotItem item;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
