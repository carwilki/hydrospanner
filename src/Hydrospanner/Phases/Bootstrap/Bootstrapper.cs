namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using System.Configuration;
	using System.Data.Common;
	using System.IO.Abstractions;
	using System.Threading;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;
	using Hydrospanner.Configuration;
	using Hydrospanner.Messaging.Rabbit;
	using Hydrospanner.Persistence;
	using Hydrospanner.Persistence.SqlPersistence;
	using Hydrospanner.Phases.Journal;
	using Hydrospanner.Phases.Snapshot;
	using Hydrospanner.Phases.Transformation;
	using Hydrospanner.Serialization;

	public class Bootstrapper : IDisposable
	{
		public void Start()
		{
			//var info = this.persistence.Restore(); // BootstrapInfo
			//info = this.snapshots.RestoreSnapshots(info, this.repository);

			//// startup the normal Snapshot and Journal ring buffers. (these are stateful and must be disposed at the end)
			//this.messages.Restore(info, snapshotRing, journalRing, this.repository);

			//// now start the normal transformation ring (this is stateful and must be disposed at the end)

			//// start the message listener
			//// this.messageReceiver = this.messaging.CreateMessageReceiver(). // TODO:....
		}

		public Bootstrapper(
			IRepository repository,
			DisruptorFactory disruptors,
			PersistenceBootstrapper persistence,
			SnapshotBootstrapper snapshots,
			MessageBootstrapper messages,
			MessagingFactory messaging)
		{
			this.repository = repository;
			this.disruptors = disruptors;
			this.persistence = persistence;
			this.snapshots = snapshots;
			this.messages = messages;
			this.messaging = messaging;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			////if (!disposing || this.disposed)
			////	return;
			////this.started = false;
			////this.disposed = true;

			/// this.messageListener = this.messageListener.TryDispose();
		}
		
		private readonly IRepository repository;
		readonly DisruptorFactory disruptors;
		readonly PersistenceBootstrapper persistence;
		readonly SnapshotBootstrapper snapshots;
		readonly MessageBootstrapper messages;
		readonly MessagingFactory messaging;
		private bool started;
		private bool disposed;
	}
}