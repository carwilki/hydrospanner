namespace Hydrospanner.Phases.Snapshot
{
	using System.Collections.Generic;
	using Machine.Specifications;

	public static class ShouldExtensions
	{
		public static void ShouldBeEqual(this KeyValuePair<string, byte[]> pair, KeyValuePair<string, byte[]> other)
		{
			pair.Key.ShouldEqual(other.Key);
			pair.Value.ShouldBeLike(other.Value);
		}
	}
}