namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;

	internal static class StringExtensions
	{
		public static string FormatWith(this string template, params object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, template, args);
		}
	}

	internal static class ByteConversionExtensions
	{
		public static int SliceInt32(this byte[] array, int index)
		{
			return array.Slice(index, 4).ToInt32();
		}

		public static string SliceString(this byte[] array, int index, int length = -1)
		{
			if (length < 0)
				length = array.Length - index;

			return array.Slice(index, length).ToUtf8String();
		}

		private static byte[] Slice(this byte[] array, int start, int length)
		{
			var buffer = new byte[length];
			Array.Copy(array, start, buffer, 0, length);
			return buffer;
		}

		private static string ToUtf8String(this byte[] array)
		{
			return Encoding.UTF8.GetString(array);
		}

		private static int ToInt32(this byte[] array)
		{
			return BitConverter.ToInt32(array, 0);
		}

		public static byte[] ToByteArray(this string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}

		public static byte[] ToByteArray(this int value)
		{
			return BitConverter.GetBytes(value);
		}
	}

	internal static class DisposableExtensions
	{
		public static T TryDispose<T>(this T resource) where T : class, IDisposable
		{
			if (resource == null)
				return null;

			try
			{
				resource.Dispose();
				return null;
			}
			catch
			{
				return null;
			}
		}
	}

	internal static class CollectionExtensions
	{
		public static TValue ValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> collection, TKey key)
		{
			TValue value;
			return collection.TryGetValue(key, out value) ? value : default(TValue);
		}
	}
}