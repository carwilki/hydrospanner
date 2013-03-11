namespace TestHarness
{
	using System;
	using System.Collections.Generic;
	using Accounting.Events;
	using Hydrospanner;

	internal static class Program
	{
		private static void Main()
		{
			using (var bootstrapper = new Bootstrapper(new TestSelector(), "Hydrospanner"))
			{
				bootstrapper.Start();
				Console.WriteLine("Press any key to exit.");
				Console.ReadLine();
				Console.WriteLine("Shutting down");
			}
		}
	}

	public class TestSelector : IHydratableSelector
	{
		public IEnumerable<IHydratableKey> Keys(object message, Dictionary<string, string> headers = null)
		{
			return new[] { new TestKey() };
		}
		public IHydratable Create(object memento)
		{
			return null; // TODO
		}
	}

	public class TestKey : IHydratableKey
	{
		public string Name { get { return "test-key"; } }
		public IHydratable Create()
		{
			return new TestHydratable();
		}
	}

	public class TestHydratable : IHydratable, IHydratable<AccountClosedEvent>
	{
		public string Key { get { return "test-key"; } }
		public bool IsComplete { get { return false; } }
		public bool PublicSnapshot { get { return false; } }
		public IEnumerable<object> GatherMessages()
		{
			yield return this.stored;
		}

		public object GetMemento()
		{
			return new TestHydratableMemento
			{
				Key = this.Key,
				Counter = this.counter,
				Event = this.stored
			};
		}

		public void Hydrate(AccountClosedEvent message, Dictionary<string, string> headers, bool live)
		{
			if (++this.counter % 10000 == 0)
				Console.WriteLine(this.counter);
			this.stored = message;
		}

		private AccountClosedEvent stored;
		private long counter;
	}

	public class TestHydratableMemento
	{
		public string Key { get; set; }
		public long Counter { get; set; }
		public AccountClosedEvent Event { get; set; }
	}
}