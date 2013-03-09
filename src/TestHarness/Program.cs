namespace TestHarness
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner;

	internal static class Program
	{
		private static void Main()
		{
			using (var bootstrapper = new Bootstrapper(null, "Hydrospanner"))
			{
				bootstrapper.Start();
				Console.WriteLine("Press any key to exit.");
				Console.ReadLine();
				Console.WriteLine("Shutting down");
			}
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