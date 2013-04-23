namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;

	public interface ITransformer
	{
		IEnumerable<object> Transform<T>(Delivery<T> delivery);
	}
}