namespace Hydrospanner
{
	using System;

	internal static class ExtensionMethods
	{
		public static DateTime ToDateTime(this long epochTime)
		{
			return EpochTime + TimeSpan.FromMilliseconds(epochTime);
		}

		public static IDisposable TryDispose(this IDisposable resource)
		{
			try
			{
				resource.Dispose();
				return resource;
			}
			catch
			{
				return resource;
			}
		}

		public static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	}
}