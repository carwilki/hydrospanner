namespace SampleApplication
{
	using System;
	using Hydrospanner.Wireup;

	internal class Program
	{
		private static void Main()
		{
			using (new Wireup(new ConventionWireupParameters()).Start())
			{
				Console.Write("<ENTER> to quit: ");
				Console.ReadLine();
				Console.WriteLine("Shutting down...");
			}
		}
	}
}
