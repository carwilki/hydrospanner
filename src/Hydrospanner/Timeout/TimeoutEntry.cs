namespace Hydrospanner.Timeout
{
	using System;
	using System.Runtime.Serialization;

	[DataContract]
	public sealed class TimeoutEntry
	{
		[DataMember(Name = "key")] public string Key { get; set; }
		[DataMember(Name = "timeout")] public DateTime Timeout { get; set; }
		[DataMember(Name = "state")] public int State { get; set; }

		public TimeoutEntry()
		{
		}
		public TimeoutEntry(TimeoutRequestedEvent message) : this(message.Key, message.Timeout, message.State)
		{
		}
		public TimeoutEntry(string key, DateTime timeout, int state)
		{
			this.Key = key;
			this.Timeout = timeout;
			this.State = state;
		}
	}
}