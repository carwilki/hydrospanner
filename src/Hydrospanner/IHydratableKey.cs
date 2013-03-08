namespace Hydrospanner
{
	public interface IHydratableKey
	{
		string Name { get; }
		IHydratable Create();
	}
}