namespace SampleApplication
{
	using System;
	using Hydrospanner.Wireup;

	public class EdgeRunner : IDisposable
	{
		public static IDisposable Initialize()
		{
			Console.WriteLine("Initializing application.");
			return new EdgeRunner();
		}
		public virtual void Execute()
		{
			Console.WriteLine("Running application.");
			this.resource.Start();
			Console.WriteLine("Press CTRL-C or CTRL-Break to exit.");
		}

		public EdgeRunner()
		{
			this.resource = Wireup.Initialize();
		}
		~EdgeRunner()
		{
			this.Dispose(false);
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

			Console.WriteLine("Shutting down application.");
			this.resource.Dispose();
		}

		private readonly Wireup resource;
	}
}