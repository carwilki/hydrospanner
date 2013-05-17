namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using Machine.Specifications;

	public static class ShouldExtensions
	{
		public static void ShouldBeEqual(this Tuple<string, string, byte[]> pair, Tuple<string, string, byte[]> other)
		{
			pair.Item1.ShouldEqual(other.Item1);
			pair.Item2.ShouldEqual(other.Item2);
			pair.Item3.ShouldEqual(other.Item3);
		}
	}
}