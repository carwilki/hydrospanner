namespace Hydrospanner.Timeout
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	[DataContract]
	public class TimeoutMemento
	{
		public TimeoutMemento(SortedList<DateTime, HashSet<string>> source)
		{
			this.Timeouts = new Dictionary<DateTime, List<string>>(source.Count);
			foreach (var item in source)
			{
				var keys = item.Value;
				var list = new List<string>(keys.Count);
				list.AddRange(keys);
				this.Timeouts[item.Key] = list;
			}
		}
		public TimeoutMemento()
		{
			this.Timeouts = new Dictionary<DateTime, List<string>>();
		}

		public void CopyTo(SortedList<DateTime, HashSet<string>> destination)
		{
			destination.Clear();

			foreach (var item in this.Timeouts)
			{
				HashSet<string> keys;
				if (!destination.TryGetValue(item.Key, out keys))
					destination[item.Key] = keys = new HashSet<string>();

				foreach (var key in item.Value)
					keys.Add(key);
			}
		}

		[DataMember]
		public Dictionary<DateTime, List<string>> Timeouts { get; private set; }
	}
}