namespace TestHarness
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using Hydrospanner;

	internal static class Program
	{
		private static void Main()
		{
			var sw = new Stopwatch();
			IHydratable test = new TestHydratable();

			var count = 10000000;
			sw.Start();
			for (int i = 0; i < count; i++)
				test.Hydrate(1, null, false);

			sw.Stop();

			Console.WriteLine(sw.Elapsed);
			Console.ReadLine();
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
			// Console.WriteLine("Hello, World!");
		}
	}
}