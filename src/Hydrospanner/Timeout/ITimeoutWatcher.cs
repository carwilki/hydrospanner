namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;

	public interface ITimeoutWatcher
	{
		void AddRange(string key, ICollection<DateTime> instants);
		void Remove(string key);
	}
}