namespace Hydrospanner.Timeout
{
	using System;
	using System.Runtime.Serialization;

	[DataContract]
	public sealed class TimeoutEntry
	{
		[DataMember(Name = "key")] public string Key { get; set; }
		[DataMember(Name = "timeout")] public DateTime Timeout { get; set; }

		public TimeoutEntry()
		{
		}
		public TimeoutEntry(TimeoutRequestedEvent message) : this(message.Key, message.Timeout)
		{
		}
		public TimeoutEntry(string key, DateTime timeout)
		{
			this.Key = key;
			this.Timeout = timeout;
		}
	}
}