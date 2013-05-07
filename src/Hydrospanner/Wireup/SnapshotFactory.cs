namespace Hydrospanner.Wireup
{
	using System;
	using System.Configuration;
	using System.IO.Abstractions;
	using Phases.Snapshot;

	public class SnapshotFactory
	{
		public virtual SystemSnapshotStreamReader CreateSystemSnapshotStreamReader(long journaledSequence)
		{
			var loader = new SystemSnapshotLoader(new DirectoryWrapper(), new FileWrapper(), this.systemSnapshotPath);
			return loader.Load(journaledSequence);
		}
		public virtual ISnapshotRecorder CreateSystemSnapshotRecorder()
		{
			return new SystemSnapshotRecorder(new FileWrapper(), this.systemSnapshotPath);
		}
		public virtual ISnapshotRecorder CreatePublicSnapshotRecorder()
		{
			if (this.settings.ConnectionString == "null-storage")
				return new NullSnapshotRecorder();

			return new PublicSnapshotRecorder(this.settings);
		}

		public SnapshotFactory(string systemSnapshotPath, string publicSnapshotConnectionName)
		{
			if (string.IsNullOrWhiteSpace(systemSnapshotPath))
				throw new ArgumentNullException("systemSnapshotPath");

			if (string.IsNullOrWhiteSpace(publicSnapshotConnectionName))
				throw new ArgumentNullException("publicSnapshotConnectionName");

			this.settings = ConfigurationManager.ConnectionStrings[publicSnapshotConnectionName];
			if (this.settings == null)
				throw new ConfigurationErrorsException("No persistence configuration info found for connection named '{0}'.".FormatWith(publicSnapshotConnectionName));

			if (string.IsNullOrWhiteSpace(this.settings.ProviderName) || string.IsNullOrWhiteSpace(this.settings.ConnectionString))
				throw new ConfigurationErrorsException("Connection named '{0}' missing provider info or connection string info.".FormatWith(publicSnapshotConnectionName));

			this.systemSnapshotPath = systemSnapshotPath;
		}
		protected SnapshotFactory()
		{
		}

		private readonly ConnectionStringSettings settings;
		private readonly string systemSnapshotPath;
	}
}