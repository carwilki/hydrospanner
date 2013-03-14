namespace Hydrospanner.Messaging.Rabbit
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;

	internal static class ExtensionMethods
	{
		public static string ToMessageId(this long sequence, short nodeId)
		{
			return ((sequence << 16) + nodeId).ToString(CultureInfo.InvariantCulture);
		}
		public static IDictionary CopyTo(this IDictionary<string, string> source, IDictionary target)
		{
			target = target ?? new Hashtable(source.Count);
			foreach (var item in source)
				target[item.Key] = item.Value;

			return target;
		}
		public static string NormalizeType(this string value)
		{
			return value.Trim().ToLowerInvariant().Replace(".", "-");
		}
	}
}