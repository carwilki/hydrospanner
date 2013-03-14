namespace Hydrospanner.Messaging
{
	using System;

	internal interface IMessageReceiver : IDisposable
	{
		MessageDelivery Receive(TimeSpan timeout);
	}
}