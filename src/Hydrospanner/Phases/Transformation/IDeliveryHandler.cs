namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;

	public interface IDeliveryHandler
	{
		IEnumerable<object> Deliver(TransformationItem item, bool live);
		IEnumerable<object> Deliver(object message, long sequence);
	}
}