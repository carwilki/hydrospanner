namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;

	public interface IDeliveryHandler
	{
		IEnumerable<object> Deliver(object message, Dictionary<string, string> headers, long sequence, bool live);
	}
}