namespace Hydrospanner
{
	using System;
	using System.Globalization;

	public static class StringExtensions
	{
		public static string FormatWith(this string template, params object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, template, args);
		}
	}

	public static class EnumerationExtensions
	{
		public static bool Includes<T>(this Enum type, T value)
		{
			return ((int)(object)type & (int)(object)value) == (int)(object)value;
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
}