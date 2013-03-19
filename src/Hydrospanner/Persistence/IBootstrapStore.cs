namespace Hydrospanner.Persistence
{
	public interface IBootstrapStore
	{
		BootstrapInfo Load();
	}
}