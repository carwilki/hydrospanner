namespace Hydrospanner.Inbox
{
	public class WarmupListener
	{
		public void Start()
		{
			// push all messages from bookmark forward into the ring
			// invoke this.listener.Start()
		}

		public void Stop()
		{
		}

		private readonly MessageListener listener;
	}
}