namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
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

			Console.WriteLine("Starting...");

			new Thread(() =>
			{
				var journalCheckpoint = this.storage.LoadJournalCheckpoint();
				var dispatchCheckpoint = this.storage.LoadDispatchCheckpoint();

				var keys = this.storage.LoadWireIdentifiers(journalCheckpoint, MaxDuplicates);
				foreach (var item in keys)
					this.duplicates.Contains(item);

				var repo = new Dictionary<string, IHydratable>();

				this.receivingDisruptor
					.HandleEventsWith(new SerializationHandler(), new DuplicateHandler(this.duplicates))
					.Then(new TransformationHandler(repo, this.snapshotRing, SnapshotFrequence, this.selector, journalCheckpoint))
					.Then(new ForwardLocalHandler(this.receivingRing, this.dispatchRing));
				this.dispatchDisruptor
					.HandleEventsWith(new SerializationHandler())
					.HandleEventsWith(new JournalHandler(this.settings))
					.HandleEventsWith(new DispatchHandler(0, dispatchCheckpoint), new AcknowledgementHandler())
					.HandleEventsWith(new CheckpointHandler(this.storage));
				this.snapshotDisruptor.HandleEventsWith(new SnapshotHandler());

				this.receivingDisruptor.Start();
				this.dispatchDisruptor.Start();
				this.snapshotDisruptor.Start();

				// TODO: when snapshot behavior exists, stream mementos from into hydratables and into repository
				var outstanding = this.storage.LoadSinceCheckpoint(0); // when snapshots work, we will load from that point
				foreach (var message in outstanding)
					PublishToRing(receivingRing, message);

				// TODO: dispatcher need to push journaled messages out
				// problem: does the ForwardLocalHandler cause problems related to duplicate messages during the startup
				// phase where journaled messages from dispatch checkpoint forward pushed to the wire?

				this.listener.Start(); // now start listening on the wire

				Console.WriteLine("Startup Complete");
			}).Start();
		}
		private static void PublishToRing(RingBuffer<WireMessage> ring, JournaledMessage journaled)
		{
			var claimed = ring.Next();
			var message = ring[claimed];
			message.Clear();

			message.MessageSequence = journaled.MessageSequence;
			message.LiveMessage = false;
			message.SerializedBody = journaled.SerializedBody;
			message.SerializedHeaders = journaled.SerializedHeaders;

			ring.Publish(claimed);
		}

		public Bootstrapper(IHydratableSelector selector, string connectionName)
		{
			this.selector = selector;
			this.settings = ConfigurationManager.ConnectionStrings[connectionName];

			this.receivingDisruptor = BuildDisruptor<WireMessage>(new MultiThreadedLowContentionClaimStrategy(PreallocatedSize));
			this.dispatchDisruptor = BuildDisruptor<DispatchMessage>(new SingleThreadedClaimStrategy(PreallocatedSize));
			this.snapshotDisruptor = BuildDisruptor<SnapshotMessage>(new SingleThreadedClaimStrategy(PreallocatedSize));
			this.receivingRing = this.receivingDisruptor.RingBuffer;
			this.dispatchRing = this.dispatchDisruptor.RingBuffer;
			this.snapshotRing = this.snapshotDisruptor.RingBuffer;

			this.storage = new MessageStore(this.settings);
			this.duplicates = new DuplicateStore(MaxDuplicates);
			this.listener = new MessageListener(this.receivingDisruptor.RingBuffer);
		}
		private static Disruptor<T> BuildDisruptor<T>(IClaimStrategy strategy) where T : class, new()
		{
			return new Disruptor<T>(() => new T(), strategy, new SleepingWaitStrategy(), TaskScheduler.Default);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			if (!this.started)
				return;

			Console.WriteLine("Stopping message listener.");
			this.started = false;
			this.listener.Dispose();
			Thread.Sleep(TimeSpan.FromSeconds(1));
			Console.WriteLine("Stopping disruptors.");
			this.receivingDisruptor.Shutdown();
			Thread.Sleep(TimeSpan.FromSeconds(1));
			this.dispatchDisruptor.Shutdown();
			Thread.Sleep(TimeSpan.FromSeconds(1));
			this.snapshotDisruptor.Shutdown();
		}

		private const int SnapshotFrequence = 100;
		private const int MaxDuplicates = 1024 * 64;
		private const int PreallocatedSize = 1024 * 32;
		private readonly ConnectionStringSettings settings;
		private readonly Disruptor<WireMessage> receivingDisruptor;
		private readonly Disruptor<DispatchMessage> dispatchDisruptor;
		private readonly Disruptor<SnapshotMessage> snapshotDisruptor;
		private readonly RingBuffer<WireMessage> receivingRing;
		private readonly RingBuffer<DispatchMessage> dispatchRing;
		private readonly RingBuffer<SnapshotMessage> snapshotRing;
		private readonly IHydratableSelector selector;
		private readonly MessageStore storage;
		private readonly DuplicateStore duplicates;
		private readonly MessageListener listener;
		private bool started;
	}
}