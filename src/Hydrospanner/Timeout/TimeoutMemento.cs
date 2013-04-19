namespace Hydrospanner.Timeout
{
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	[DataContract]
	public sealed class TimeoutMemento
	{
		[DataMember(Name = "timeouts")]
		public List<TimeoutEntry> Timeouts { get; set; }
	}
}