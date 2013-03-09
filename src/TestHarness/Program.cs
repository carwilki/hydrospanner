namespace TestHarness
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner;

	internal static class Program
	{
		private static void Main()
		{
			IHydratable test = new TestHydratable();
			test.Hydrate(1, null);
		}
	}

	public class TestHydratable : IHydratable, IHydratable<int>
	{
		public string Key { get; private set; }
		public bool IsComplete { get; private set; }
		public IEnumerable<object> GatherMessages()
		{
			return null;
		}

		public object GetMemento()
		{
			return null;
		}

		public void LoadFromMemento(object memento)
		{
		}

		public void Hydrate(int message, Dictionary<string, string> headers, bool replay)
		{
			Console.WriteLine("Hello, World!");
		}
	}
}