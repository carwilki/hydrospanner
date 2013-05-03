namespace Hydrospanner.Persistence
{
	public class GraveyardMemento
	{
		public string[] Keys { get; private set; }

		public GraveyardMemento(string[] keys)
		{
			this.Keys = keys;
		}
	}
}