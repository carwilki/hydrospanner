namespace Hydrospanner.Messaging
{
	using System;

	public interface IMessageReceiver : IDisposable
	{
		MessageDelivery Receive(TimeSpan timeout);
	}
}