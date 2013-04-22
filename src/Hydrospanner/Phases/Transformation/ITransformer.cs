namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;

	public interface ITransformer
	{
		IEnumerable<object> Handle<T>(Delivery<T> delivery);
	}
}