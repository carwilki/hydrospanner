namespace Hydrospanner.Messaging
{
	using System;
	using Hydrospanner.Phases.Transformation;

	public interface IMessageReceiver : IDisposable
	{
		void Receive(Action<TransformationItem> callback, TimeSpan timeout);
	}
}