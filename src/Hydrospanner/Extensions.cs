namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Globalization;
	using System.Text;
	using System.Threading;

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

		public static TValue Add<TKey, TValue>(this IDictionary<TKey, TValue> collection, TKey key, Func<TValue> factory)
		{
			TValue value;
			if (!collection.TryGetValue(key, out value))
				collection[key] = value = factory();

			return value;
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

	internal static class DataExtensions
	{
		public static IDbConnection OpenConnection(this ConnectionStringSettings connectionSettings)
		{
			if (connectionSettings == null)
				throw new ArgumentNullException("connectionSettings");

			var provider = DbProviderFactories.GetFactory(connectionSettings.ProviderName);
			var connection = provider.CreateConnection();

			if (connection == null)
				throw new ConfigurationErrorsException(string.Format("The connection named \"{0}\" could not be created.", connectionSettings.Name));

			connection.ConnectionString = connectionSettings.ConnectionString;
			connection.Open();
			return connection;
		}
		public static IDbCommand WithParameter(this IDbCommand command, string name, object value, DbType type)
		{
			try
			{
				var parameter = command.CreateParameter();
				parameter.ParameterName = name;
				parameter.Value = value ?? DBNull.Value;
				parameter.DbType = type;
				command.Parameters.Add(parameter);
				return command;
			}
			catch
			{
				command.Dispose();
				throw;
			}
		}
	}
}