namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;

	public static class ShouldExtensions
	{
		public static void ShouldBeEqual(this KeyValuePair<Type, byte[]> pair, KeyValuePair<Type, byte[]> other)
		{
			pair.Key.ShouldEqual(other.Key);
			pair.Value.ShouldBeLike(other.Value);
		}
	}
}