namespace Hydrospanner
{
	using System;
	using System.Configuration;
	using System.Threading;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;

	public class Bootstrapper : IDisposable
	{
		public void Start()
		{
			if (this.started)
				return;

			this.started = true;

			new Thread(() =>
			{
				var keys = this.storage.LoadWireIdentifiers(MaxDuplicates);
				foreach (var item in keys)
					this.duplicates.Contains(item);

				var ring = this.input.Start();
				var outstanding = this.storage.LoadSinceCheckpoint();

				if (outstanding.Count > 0)
					this.checkpointAtStartup = outstanding[0].Sequence - 1;

				foreach (var message in outstanding)
					PublishToRing(ring, message);

				this.listener.Start();
			}).Start();
		}
		private static void PublishToRing(RingBuffer<WireMessage> ring, JournaledMessage journaled)
		{
			var claimed = ring.Next();
			var message = ring[claimed];
			message.Clear();

			message.MessageSequence = journaled.Sequence;
			message.WireId = journaled.WireId;
			message.SerializedBody = journaled.SerializedBody;
			message.SerializedHeaders = journaled.SerializedHeaders;

			ring.Publish(claimed);
		}

		public Bootstrapper(IStreamIdentifier identifier, string connectionName)
		{
			var settings = ConfigurationManager.ConnectionStrings[connectionName];

			this.storage = new MessageStore(settings);
			this.duplicates = new DuplicateStore(MaxDuplicates);
			this.input = BuildDisruptor<WireMessage>();
	        this.input
	            .HandleEventsWith(new SerializationHandler())
				.Then(new IdentificationHandler(identifier, this.duplicates))
	            .Then(new JournalHandler(connectionName))
	            .Then(new AcknowledgementHandler());

			this.listener = new MessageListener(this.input.RingBuffer);
		}
		private static Disruptor<T> BuildDisruptor<T>() where T : class, new()
		{
			return new Disruptor<T>(
				() => new T(),
				new SingleThreadedClaimStrategy(PreallocatedSize), 
				new SleepingWaitStrategy(),
				TaskScheduler.Default);
		}

		public void Dispose()
		{
			this.input.Shutdown();
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			if (!this.started)
				return;

			this.started = false;
			this.listener.Stop();
			Thread.Sleep(TimeSpan.FromSeconds(1)); // TODO: optimize this
			this.input.Shutdown();
		}

		private const int MaxDuplicates = 1024 * 32;
		private const int PreallocatedSize = 1024 * 16;
		private readonly MessageStore storage;
		private readonly DuplicateStore duplicates;
		private readonly Disruptor<WireMessage> input;
		private readonly MessageListener listener;
		private bool started;
		private long checkpointAtStartup; // now we know if a given message has been handled before or if it's the first occurrence.
	}
}