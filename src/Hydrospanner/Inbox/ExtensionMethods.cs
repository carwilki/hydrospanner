﻿namespace Hydrospanner.Inbox
{
	using System;

	internal static class ExtensionMethods
	{
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
	}
}