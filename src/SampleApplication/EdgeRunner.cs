namespace SampleApplication
{
	using System;
	using Hydrospanner.Wireup;

	public class EdgeRunner
	{
		public static IDisposable Initialize()
		{
			return Wireup.Initialize(typeof(FizzBuzzAggregate).Assembly);
		}
	}
}