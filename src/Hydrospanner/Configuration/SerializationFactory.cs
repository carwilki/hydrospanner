namespace Hydrospanner.Configuration
{
	using Hydrospanner.Serialization;

	public class SerializationFactory
	{
		public virtual ISerializer CreateSerializer
		{
			get { return new JsonSerializer(); }
		}
	}
}