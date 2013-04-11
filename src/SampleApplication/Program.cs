namespace SampleApplication
{
	using System;
	using Hydrospanner.Wireup;

	internal class Program
	{
		private static void Main()
		{
			using (Wireup.Start())
			{
				Console.Write(@"<ENTER> to quit: ");
				Console.ReadLine();
				Console.WriteLine(@"Shutting down...");
			}
		}
	}
}
