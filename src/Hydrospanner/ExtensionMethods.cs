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

	public static class ReflectionExtensions
	{
		public static void Hydrate(this IHydratable hydratable, object message, Dictionary<string, string> headers, bool replay)
		{
			// TODO: http://msmvps.com/blogs/jon_skeet/archive/2008/08/09/making-reflection-fly-and-exploring-delegates.aspx
			if (message == null)
				return;

			var type = message.GetType();
			MethodInfo method;
			if (!Cache.TryGetValue(type, out method))
				Cache[type] = method = typeof(IHydratable<>).MakeGenericType(type).GetMethods()[0];

			method.Invoke(hydratable, new[] { message, headers, replay });
		}

		private static readonly Dictionary<Type, MethodInfo> Cache = new Dictionary<Type, MethodInfo>();
	}
}