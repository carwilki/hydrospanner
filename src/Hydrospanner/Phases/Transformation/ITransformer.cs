namespace Hydrospanner.Phases.Transformation
{
	using System.Collections.Generic;

	public interface ITransformer
	{
		IEnumerable<object> Handle(object message, Dictionary<string, string> headers, long sequence);
	}
}