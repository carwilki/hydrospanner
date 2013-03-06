namespace TestHarness
{
	using System;
	using Hydrospanner;

	internal static class Program
	{
		private static void Main()
		{
			var identifier = new TestStreamIdentifier();

			using (var bootstrapper = new Bootstrapper(identifier, ConnectionName))
			{
				bootstrapper.Start();
				Console.WriteLine("Press enter");
				Console.ReadLine();
			}
		}

	    private const string ConnectionName = "Hydrospanner";
	}
}