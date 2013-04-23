namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;

	public sealed class NullTimeoutWatcher : ITimeoutWatcher
	{
		public void AddRange(string key, ICollection<DateTime> instants)
		{
		}
		public void Remove(string key)
		{
		}
	}
}