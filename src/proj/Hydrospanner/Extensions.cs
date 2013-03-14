namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;

	public static class StringExtensions
	{
		public static string FormatWith(this string template, params object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, template, args);
		}
	}

	public static class DisposableExtensions
	{
		public static void TryDispose(this IDisposable resource)
		{
			if (resource == null)
				return;

			try
			{
				resource.Dispose();
			}
			catch
			{
				return;
			}
		}
	}

	public static class CollectionExtensions
	{
		public static TValue ValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> collection, TKey key)
		{
			TValue value;
			return collection.TryGetValue(key, out value) ? value : default(TValue);
		}
	}
}