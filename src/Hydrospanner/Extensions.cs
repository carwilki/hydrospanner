namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using System.Text;
	using System.Threading;

	internal static class ReflectionExtensions
	{
		public static string ResolvableTypeName(this object value)
		{
			return value == null ? null : value.GetType().ResolvableTypeName();
		}
		public static string ResolvableTypeName(this Type type)
		{
			return type == null ? null : "{0}, {1}".FormatWith(type.FullName, type.Assembly.GetName().Name);
		}
	}

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

		public static uint ComputeHash(this byte[] data)
		{
			// Reference: http://stackoverflow.com/a/425184/605022
			unchecked
			{
				uint result = 0;
				if (data != null)
					for (var i = 0; i < data.Length; i++)
						result = (result * 31) ^ data[i];

				return result;
			}
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

		public static TValue Add<TKey, TValue>(this IDictionary<TKey, TValue> collection, TKey key, Func<TValue> factory)
		{
			TValue value;
			if (!collection.TryGetValue(key, out value))
				collection[key] = value = factory();

			return value;
		}

		public static void TryClear<T>(this ICollection<T> collection)
		{
			if (collection != null && !collection.IsReadOnly)
				collection.Clear();
		}
	}

	internal static class ThreadExtensions
	{
		public static void Sleep(this TimeSpan sleep)
		{
			callback(sleep);
		}

		public static void Freeze(Action<TimeSpan> action)
		{
			callback = action;
		}
		public static void Unfreeze()
		{
			callback = Thread.Sleep;
		}

		private static Action<TimeSpan> callback = Thread.Sleep;
	}
}