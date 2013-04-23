namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;

	public interface ITimeoutHydratable : IHydratable<TimeoutMessage>
	{
		ICollection<DateTime> Timeouts { get; }
	}
}