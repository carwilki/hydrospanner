namespace Hydrospanner.Transformation
{
	using Disruptor;

	public class TransformationHandler : IEventHandler<TransformationMessage>
	{
		public void OnNext(TransformationMessage data, long sequence, bool endOfBatch)
		{
			// push message into hydratables
			// gather resulting state (messages/snapshots?) and push to next phase (as a batch)

			// TODO: figure out when to snapshot...???
			// 1. when we reach the live stream during a replay operation? (stream length = stream index)???
			// 2. when a certain number of messages have been handled???
			// 3. perhaps next phase figures out to store snapshot???
		}
	}
}