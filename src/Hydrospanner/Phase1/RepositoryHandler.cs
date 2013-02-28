namespace Hydrospanner.Phase1
{
	using System;
	using System.Collections.Generic;
	using Disruptor;
	using Hydrospanner.Phase2;

	public class RepositoryHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			var stream = this.identifier.DiscoverStreams(data.Body, data.Headers);
			List<IHydratable> hydratable;
			if (!this.cache.TryGetValue(stream, out hydratable))
				hydratable = this.factory(stream);

			var claimed = this.phase2.Next();
			var message = this.phase2[claimed];
			message.StreamId = stream;
			message.Body = data.Body;
			message.Headers = data.Headers;
			message.ConfirmDelivery = data.ConfirmDelivery;
			message.Hydratables = hydratable;
			this.phase2.Publish(claimed);
		}

		public RepositoryHandler(IStreamIdentifier identifier, Func<Guid, List<IHydratable>> factory, RingBuffer<ParsedMessage> phase2)
		{
			this.identifier = identifier;
			this.factory = factory;
			this.phase2 = phase2;
		}

		private readonly Dictionary<Guid, List<IHydratable>> cache = new Dictionary<Guid, List<IHydratable>>();
		private readonly IStreamIdentifier identifier;
		private readonly Func<Guid, List<IHydratable>> factory;
		private readonly RingBuffer<ParsedMessage> phase2;
	}
}