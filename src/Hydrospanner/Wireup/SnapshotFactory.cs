namespace Hydrospanner.Wireup
{
	using System;
	using System.Configuration;
	using System.Data.Common;
	using System.IO;
	using System.IO.Abstractions;
	using Phases.Snapshot;

	public class SnapshotFactory
	{
		public virtual SystemSnapshotStreamReader CreateSystemSnapshotStreamReader(long journaledSequence)
		{
			var loader = new SystemSnapshotLoader(new DirectoryWrapper(), new FileWrapper(), x => new FileInfoWrapper(new FileInfo(x)), this.systemSnapshotPath);
			return loader.Load(journaledSequence);
		}
		public virtual ISnapshotRecorder CreateSystemSnapshotRecorder()
		{
			return new SystemSnapshotRecorder(new DirectoryWrapper(), new FileWrapper(), this.systemSnapshotPath);
		}
		public virtual ISnapshotRecorder CreatePublicSnapshotRecorder()
		{
			if (this.connectionString == "null-storage")
				return new NullSnapshotRecorder();

			return new PublicSnapshotRecorder(this.factory, this.connectionString);
		}

		public SnapshotFactory(string systemSnapshotPath, string connectionName)
		{
			if (string.IsNullOrWhiteSpace(systemSnapshotPath))
				throw new ArgumentNullException("systemSnapshotPath");

			if (string.IsNullOrWhiteSpace(connectionName))
				throw new ArgumentNullException("connectionName");

			var settings = ConfigurationManager.ConnectionStrings[connectionName];
			if (settings == null)
				throw new ConfigurationErrorsException("No persistence configuration info found for connection named '{0}'.".FormatWith(connectionName));

			if (string.IsNullOrWhiteSpace(settings.ProviderName) || string.IsNullOrWhiteSpace(settings.ConnectionString))
				throw new ConfigurationErrorsException("Connection named '{0}' missing provider info or connection string info.".FormatWith(connectionName));

			this.factory = DbProviderFactories.GetFactory(settings.ProviderName);
			this.connectionString = settings.ConnectionString;
			this.systemSnapshotPath = systemSnapshotPath;
		}
		protected SnapshotFactory()
		{
		}

		private readonly string systemSnapshotPath;
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
	}
}