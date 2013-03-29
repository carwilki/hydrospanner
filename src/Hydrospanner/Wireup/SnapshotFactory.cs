namespace Hydrospanner.Wireup
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
			var loader = new SystemSnapshotLoader(new DirectoryWrapper(), new FileWrapper(), this.systemSnapshotPath);
			return loader.Load(journaledSequence, this.snapshotGeneration);
		}
		public virtual ISnapshotRecorder CreateSystemSnapshotRecorder()
		{
			return new SystemSnapshotRecorder(new FileWrapper(), this.systemSnapshotPath);
		}
		public virtual ISnapshotRecorder CreatePublicSnapshotRecorder()
		{
			return new PublicSnapshotRecorder(this.settings);
		}

		public SnapshotFactory(int snapshotGeneration, string systemSnapshotPath, string publicSnapshotConnectionName)
		{
			if (string.IsNullOrWhiteSpace(systemSnapshotPath))
				throw new ArgumentNullException("systemSnapshotPath");

			if (snapshotGeneration < 0)
				throw new ArgumentOutOfRangeException("snapshotGeneration");

			if (string.IsNullOrWhiteSpace(publicSnapshotConnectionName))
				throw new ArgumentNullException("publicSnapshotConnectionName");

			this.settings = ConfigurationManager.ConnectionStrings[publicSnapshotConnectionName];
			if (this.settings == null)
				throw new ConfigurationErrorsException("No persistence configuration info found for connection named '{0}'.".FormatWith(publicSnapshotConnectionName));

			if (string.IsNullOrWhiteSpace(this.settings.ProviderName) || string.IsNullOrWhiteSpace(this.settings.ConnectionString))
				throw new ConfigurationErrorsException("Connection named '{0}' missing provider info or connection string info.".FormatWith(publicSnapshotConnectionName));

			this.snapshotGeneration = snapshotGeneration;
			this.systemSnapshotPath = systemSnapshotPath;
		}
		protected SnapshotFactory()
		{
		}

		private readonly ConnectionStringSettings settings;
		private readonly int snapshotGeneration;
		private readonly string systemSnapshotPath;
	}
}