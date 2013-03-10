﻿namespace TestHarness
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
		public string Key { get; private set; }
		public bool IsComplete { get; private set; }
		public int SnapshotFrequency { get; private set; }
		public IEnumerable<object> GatherMessages()
		{
			yield return this.stored;
		}

		public object GetMemento()
		{
			return null;
		}

		public void LoadFromMemento(object memento)
		{
		}

		public void Hydrate(AccountClosedEvent message, Dictionary<string, string> headers, bool live)
		{
			Console.WriteLine("Hello, World!");
			this.stored = message;
		}

		private AccountClosedEvent stored;
	}
}