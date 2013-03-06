namespace Hydrospanner
{
	using System.Collections.Generic;

	public interface IMessage
	{
		byte[] SerializedBody { get; set; }
		byte[] SerializedHeaders { get; set; }

		object Body { get; set; }
		Dictionary<string, string> Headers { get; set; }
	}
}