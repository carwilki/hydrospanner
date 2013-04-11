namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;

	internal static class ExtensionMethods
	{
		public static string ToMessageId(this long sequence, short nodeId)
		{
			return ((sequence << 16) + nodeId).ToString(CultureInfo.InvariantCulture);
		}
		public static Guid ToMessageId(this string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return Guid.NewGuid(); // all messages must have an id so we can determine @ startup which are local and which are foreign

			Guid guid;
			if (Guid.TryParse(value, out guid))
				return guid;

			long numeric;
			if (long.TryParse(value, out numeric))
				return new Guid(0, 0, 0, BitConverter.GetBytes(numeric));

			return Guid.NewGuid(); // all messages must have an id so we can determine @ startup which are local and which are foreign
		}
		public static IDictionary CopyTo(this IDictionary<string, string> source, IDictionary target)
		{
			if (source == null)
				return target ?? new Hashtable();

			target = target ?? new Hashtable(source.Count);
			foreach (var item in source)
				target[item.Key] = item.Value;

			return target;
		}
		public static Dictionary<string, string> Copy(this IDictionary source)
		{
			source = source ?? new Hashtable();
			var target = new Dictionary<string, string>(source.Count);
			foreach (var key in source.Keys)
				target[key as string ?? string.Empty] = RabbitEncoding.GetString(source[key] as byte[] ?? new byte[0]);

			return target;
		}

		public static string NormalizeType(this string value)
		{
			var end = value.IndexOf(',');
			if (end >= 0)
				value = value.Substring(0, end);

			return value.Trim().ToLowerInvariant().Replace(".", "-");
		}

		private static readonly Encoding RabbitEncoding = Encoding.UTF8;
	}
}