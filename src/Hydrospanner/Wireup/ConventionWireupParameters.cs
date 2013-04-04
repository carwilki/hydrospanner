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
				if (!short.TryParse(RetrieveAppSetting("NodeId"), out parsed))
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
					return new Uri(RetrieveAppSetting("BrokerAddress"));
				}
				catch (Exception)
				{
					throw new ArgumentException("Please supply a valid broker address (URI).");
				}
			}
		}
		public virtual string SourceQueueName { get { return RetrieveAppSetting("SourceQueue"); } }
		public virtual string JournalConnectionName { get { return RetrieveConnectionName("MessageJournal"); } }
		public virtual string PublicSnapshotConnectionName { get { return RetrieveConnectionName("PublicSnapshots"); } }
		public virtual int DuplicateWindow { get { return RetrieveNumericAppSetting("DuplicateWindow"); } }
		public virtual int JournalBatchSize { get { return RetrieveNumericAppSetting("JournalBatchSize"); } }
		public virtual int SnapshotGeneration { get { return RetrieveNumericAppSetting("SnapshotGeneration"); } }
		public virtual string SnapshotLocation { get { return RetrieveAppSetting("SnapshotLocation"); } }
		public virtual int SystemSnapshotFrequency { get { return RetrieveNumericAppSetting("SystemSnapshotFrequency"); } }

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

		private static int RetrieveNumericAppSetting(string name)
		{
			var value = RetrieveAppSetting(name);
			int parsed;
			if (!int.TryParse(value, out parsed))
				throw new ArgumentException(
					string.Format("Please supply a value for the app setting '{0}' that can be parsed as a 4-byte integer.", name));

			return parsed;
		}
	}
}