namespace TestHarness
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Accounting.Events;
	using Disruptor;
	using Disruptor.Dsl;
	using Hydrospanner;
	using Hydrospanner.Phase1;
	using Hydrospanner.Phase2;

	internal static class Program
	{
		private static void Main()
		{
			var identifiers = new Dictionary<Type, IStreamIdentifier>();
			identifiers[typeof(AccountClosedEvent)] = new TestStreamIdentifier();

			var phase2Disruptor = BuildDisruptor<ParsedMessage>();
			phase2Disruptor
				.HandleEventsWith(new TransformationHandler())
				.Then(new AcknowledgementHandler());
			var phase2RingBuffer = phase2Disruptor.Start();

			var phase1Disruptor = BuildDisruptor<WireMessage>();
			phase1Disruptor
				.HandleEventsWith(new DeserializationHandler())
				.Then(new RepositoryHandler(new TestStreamIdentifier(), Build, phase2RingBuffer));

			using (var listener = new MessageListener(phase1Disruptor.Start()))
			{
				listener.Start();
				Console.WriteLine("Press enter");
				Console.ReadLine();
				listener.Stop();
			}
		}
		private static List<IHydratable> Build(Guid streamId)
		{
			return new List<IHydratable>(new[] { new TestHydratable() });
		}

		private static Disruptor<T> BuildDisruptor<T>() where T : class, new()
		{
			return new Disruptor<T>(
				() => new T(),
				new SingleThreadedClaimStrategy(PreallocatedSize), 
				new SleepingWaitStrategy(),
				TaskScheduler.Default);
		}

		private const int PreallocatedSize = 1024 * 4;
	}

	public class TestStreamIdentifier : IStreamIdentifier
	{
		public Guid DiscoverStreams(object message, Hashtable headers)
		{
			var closedEvent = message as AccountClosedEvent;
			return closedEvent == null ? Guid.Empty : closedEvent.AccountId;
		}
	}
	public class TestHydratable : IHydratable
	{
		public void Hydrate(object message, Hashtable headers)
		{
			Console.WriteLine("Hello, World!");
		}
	}
}