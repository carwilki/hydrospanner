namespace Hydrospanner.Messaging.Rabbit
{
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
		public static IDictionary CopyTo(this IDictionary<string, string> source, IDictionary target)
		{
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
			return value.Trim().ToLowerInvariant().Replace(".", "-");
		}

		private static readonly Encoding RabbitEncoding = Encoding.UTF8;
	}
}