namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Dynamic;
	using System.Globalization;

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
		public static IDbCommand WithCommand(
			this IDbConnection connection, string statement, params object[] numberedArgs)
		{
			var command = connection.CreateCommand();
			command.CommandText = statement;
			if (numberedArgs != null)
				foreach (var arg in numberedArgs)
					command = command.WithNumberedParameter(arg);

			return command;
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
		public static IDbCommand WithNumberedParameter(this IDbCommand command, object value)
		{
			return command.WithParameter("@" + command.Parameters.Count, value);
		}
		public static object AndExecuteScalar(this IDbCommand command)
		{
			using (command)
				return command.ExecuteScalar();
		}
		public static int AndExecuteNonQuery(this IDbCommand command)
		{
			using (command)
				return command.ExecuteNonQuery();
		}
		public static IEnumerable<IDataReader> AndExecuteReader(this IDbCommand command)
		{
			using (command)
			using (var reader = command.ExecuteReader())
			{
				if (reader == null)
					yield break;

				while (reader.Read())
					yield return reader;
			}
		}

		// Credits for the dynamic reader method: https://github.com/robconery/massive

		public static IEnumerable<dynamic> AndExecuteDynamicReader(this IDbCommand command)
		{
			using (var reader = command.ExecuteReader())
			{
				if (reader == null)
					yield break;

				while (reader.Read())
					yield return ToExpando(reader);
			}
		}
		private static dynamic ToExpando(IDataRecord record)
		{
			var e = new ExpandoObject();
			var d = e as IDictionary<string, object>;

			for (var i = 0; i < record.FieldCount; i++)
				d.Add(record.GetName(i), DBNull.Value.Equals(record[i]) ? null : record[i]);

			return e;
		}
	}
}