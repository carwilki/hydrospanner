namespace Hydrospanner.Persistence
{
	public sealed class GraveyardMemento
	{
		public string[] Keys { get; private set; }

		public GraveyardMemento(string[] keys)
		{
			this.Keys = keys;
		}
	}
}