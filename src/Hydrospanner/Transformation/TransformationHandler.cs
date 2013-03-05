namespace Hydrospanner.Transformation
{
    using System.Collections.Generic;
    using System.Linq;
    using Disruptor;
    using Outbox;

	public class TransformationHandler : IEventHandler<TransformationMessage>
	{
	    public void OnNext(TransformationMessage data, long sequence, bool endOfBatch)
		{
		    this.buffer.Add(data);

		    if (!endOfBatch)
		        return;

		    this.Transform();
		    this.buffer.Clear();

            // TODO: figure out when to snapshot...???
            // 1. when we reach the live stream during a replay operation? (stream length = stream index)???
            // 2. when a certain number of messages have been handled???
            // 3. perhaps next phase figures out to store snapshot???
		}

	    private void Transform()
	    {
	        for (var i = 0; i < this.buffer.Count; i++)
	        {
	            var data = this.buffer[i];
                foreach (var hydratable in data.Hydratables)
                {
                    hydratable.Hydrate(data.Body, null); // TODO: headers
                    if (data.StreamIndex != data.StreamLength) 
                        continue;
                    
                    var gathered = hydratable.GatherMessages().ToList();
                    var batch = this.outboxPhase.NewBatchDescriptor(gathered.Count);
                    var batchIndex = batch.Start;
                    foreach (var outgoing in gathered)
                    {
                        var message = this.outboxPhase[batchIndex++];
                        message.Body = outgoing;
                        message.Headers = null; // TODO: headers
                        message.IncomingSequence = data.IncomingSequence; // TODO: Not sure about this one...
                    }
                    this.outboxPhase.Publish(batch);
                }
	        }
	    }


	    public TransformationHandler(RingBuffer<DispatchMessage> outboxPhase)
	    {
	        this.outboxPhase = outboxPhase;
	    }

	    private readonly List<TransformationMessage> buffer = new List<TransformationMessage>();
        private readonly RingBuffer<DispatchMessage> outboxPhase;
    }
}