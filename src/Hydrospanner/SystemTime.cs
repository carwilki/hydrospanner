namespace Hydrospanner
{
	using System;

	internal static class SystemTime
	{
		public static DateTime UtcNow
		{
			get
			{
				if (cycle == null)
					return DateTime.UtcNow;

				if (index >= cycle.Length)
					index = 0;

				return cycle[index++];
			}
		}

		public static void Freeze(params DateTime[] values)
		{
			index = 0;
			cycle = values == null || values.Length == 0 ? new[] { DateTime.UtcNow } : values;
		}
		public static void Unfreeze()
		{
			index = 0;
			cycle = null;
		}

		public static int EpochUtcNow { get { return (int)((UtcNow.Ticks - 621355968000000000) / 10000000); } }

		private static int index;
		private static DateTime[] cycle;
	}

	// Credits:
	// http://ayende.com/blog/3408/dealing-with-time-in-tests
	// http://stackoverflow.com/questions/2425721/unit-testing-datetime-now
}