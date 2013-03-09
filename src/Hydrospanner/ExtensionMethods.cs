namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Globalization;
	using System.Reflection;

	internal static class DisposableExtensions
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

	internal static class StringExtensions
	{
		public static string FormatWith(this string format, params object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, format, args);
		}
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
		public static IDbCommand WithParameter(this IDbCommand command, string name, object value)
		{
			try
			{
				var parameter = command.CreateParameter();
				parameter.ParameterName = name;
				parameter.Value = value ?? DBNull.Value;
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

	internal static class HydratableExtensions
	{
		public static void Hydrate(this IHydratable hydratable, object message, Dictionary<string, string> headers, bool replay)
		{
			if (message == null)
				return;

			var type = message.GetType();
			Action<IHydratable, object, Dictionary<string, string>, bool> callback;
			if (!MethodCache.TryGetValue(type, out callback))
				MethodCache[type] = callback = MakeHydrateDelegate(type);

			callback(hydratable, message, headers, replay);
		}

		private static Action<IHydratable, object, Dictionary<string, string>, bool> MakeHydrateDelegate(Type messageType)
		{
			var method = DelegateMethod.MakeGenericMethod(messageType);
			var callback = Delegate.CreateDelegate(typeof(Action<IHydratable, object, Dictionary<string, string>, bool>), method);
			return (Action<IHydratable, object, Dictionary<string, string>, bool>)callback;
		}
		private static void HydrateDelegate<T>(this IHydratable hydratable, object message, Dictionary<string, string> headers, bool replay)
		{
			((IHydratable<T>)hydratable).Hydrate((T)message, headers, replay);
		}

		private static readonly MethodInfo DelegateMethod = typeof(HydratableExtensions).GetMethod("HydrateDelegate", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly Dictionary<Type, Action<IHydratable, object, Dictionary<string, string>, bool>> MethodCache =
			new Dictionary<Type, Action<IHydratable, object, Dictionary<string, string>, bool>>();
	}
}