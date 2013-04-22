namespace Hydrospanner.Persistence
{
	public class GraveyardMemento
	{
		public string[] Keys { get; private set; }

		public GraveyardMemento()
		{
			this.Keys = new string[0];
		}
		public GraveyardMemento(string[] keys)
		{
			this.Keys = keys;
		}
	}
}