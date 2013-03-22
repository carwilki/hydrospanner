namespace Hydrospanner.Configuration
{
	using System;
	using System.Configuration;
	using System.IO.Abstractions;
	using Phases.Snapshot;

	public class SnapshotFactory
	{
		public int SnapshotGeneration
		{
			get { return this.snapshotGeneration; }
		}

		public virtual SystemSnapshotStreamReader CreateSystemSnapshotStreamReader(long journaledSequence)
		{
			var loader = new SystemSnapshotLoader(new DirectoryWrapper(), new FileWrapper(), this.snapshotPath);
			return loader.Load(journaledSequence, this.snapshotGeneration);
		}
		public virtual ISnapshotRecorder CreateSystemSnapshotRecorder()
		{
			return new SystemSnapshotRecorder(new FileWrapper(), this.snapshotPath);
		}
		public virtual ISnapshotRecorder CreatePublicSnapshotRecorder()
		{
			return new PublicSnapshotRecorder(this.settings);
		}

		public SnapshotFactory(int snapshotGeneration, string snapshotPath, string connectionName)
		{
			if (string.IsNullOrWhiteSpace(snapshotPath))
				throw new ArgumentNullException("snapshotPath");

			if (snapshotGeneration < 0)
				throw new ArgumentOutOfRangeException("snapshotGeneration");

			if (string.IsNullOrWhiteSpace(connectionName))
				throw new ArgumentNullException("connectionName");

			this.settings = ConfigurationManager.ConnectionStrings[connectionName];
			if (this.settings == null)
				throw new ConfigurationErrorsException("No persistence configuration info found for connection named '{0}'.".FormatWith(connectionName));

			if (string.IsNullOrWhiteSpace(this.settings.ProviderName) || string.IsNullOrWhiteSpace(this.settings.ConnectionString))
				throw new ConfigurationErrorsException("Connection named '{0}' missing provider info or connection string info.".FormatWith(connectionName));

			this.snapshotGeneration = snapshotGeneration;
			this.snapshotPath = snapshotPath;
		}
		protected SnapshotFactory()
		{
		}

		private readonly ConnectionStringSettings settings;
		private readonly int snapshotGeneration;
		private readonly string snapshotPath;
	}
}