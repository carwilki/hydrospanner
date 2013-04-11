namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Specialized;
	using System.Configuration;

	public class ConventionWireupParameters
	{
		public virtual short NodeId
		{
			get
			{
				short parsed;
				if (!short.TryParse(RetrieveAppSetting("hydrospanner-node-id"), out parsed))
					throw new ArgumentException("Please supply a NodeId in the form of a parse-able 2-byte integer (short).");

				return parsed;
			}
		}
		public virtual Uri BrokerAddress
		{
			get
			{
				try
				{
					return new Uri(RetrieveAppSetting("hydrospanner-broker-address"));
				}
				catch (Exception)
				{
					throw new ArgumentException("Please supply a valid broker address (URI).");
				}
			}
		}
		public virtual string SourceQueueName { get { return RetrieveAppSetting("hydrospanner-source-queue"); } }
		public virtual string JournalConnectionName { get { return RetrieveConnectionName("hydrospanner-journal"); } }
		public virtual string PublicSnapshotConnectionName { get { return RetrieveConnectionName("hydrospanner-public-snapshots"); } }
		public virtual int DuplicateWindow { get { return RetrieveNumericAppSetting("hydrospanner-duplicate-window", 1024 * 1024); } }
		public virtual int JournalBatchSize { get { return RetrieveNumericAppSetting("hydrospanner-journal-batch-size", 1024); } }
		public virtual string SnapshotLocation { get { return RetrieveAppSetting("hydrospanner-system-snapshot-location"); } }
		public virtual int SystemSnapshotFrequency { get { return RetrieveNumericAppSetting("hydrospanner-system-snapshot-frequency", 50000); } }

		private static readonly NameValueCollection Settings = ConfigurationManager.AppSettings;
		private static readonly ConnectionStringSettingsCollection Connections = ConfigurationManager.ConnectionStrings;
		private static string RetrieveAppSetting(string name)
		{
			var value = Settings[name];
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentException(string.Format("Please supply a value for the app setting: {0}", name));

			return value;
		}
		private static string RetrieveConnectionName(string name)
		{
			var settings = Connections[name];
			if (settings == null)
				throw new ArgumentException(string.Format("Please supply a connection string settings entry called: {0}", name));

			return settings.Name;
		}

		private static int RetrieveNumericAppSetting(string name, int defaultValue = -1)
		{
			var value = RetrieveAppSetting(name);

			int parsed;
			if (int.TryParse(value, out parsed) && parsed > 0)
				return parsed;

			if (defaultValue <= 0 || parsed <= 0)
				throw new ArgumentException(
					string.Format("Please supply a value for the app setting '{0}' that can be parsed as a 4-byte integer.", name));

			return defaultValue;
		}
	}
}