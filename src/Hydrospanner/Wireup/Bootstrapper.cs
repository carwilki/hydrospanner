﻿namespace Hydrospanner.Wireup
{
	using System;
	using log4net;
	using Persistence;
	using Phases.Journal;
	using Phases.Snapshot;
	using Phases.Transformation;
	using Timeout;

	public class Bootstrapper : IDisposable
	{
		public bool Start(BootstrapInfo info)
		{
			if (this.started)
				return true;

			this.started = true;

			Log.Info("Loading mementos from latest snapshot.");
			info = this.snapshots.RestoreSnapshots(this.repository, info);
			if (info == null)
				return this.started = false;
			
			Log.Info("Starting snapshot disruptor.");
			this.snapshotDisruptor = this.disruptors.CreateSnapshotDisruptor();
			this.snapshotDisruptor.Start();

			Log.Info("Starting journal disruptor.");
			this.journalDisruptor = this.disruptors.CreateJournalDisruptor(info);
			this.journalDisruptor.Start();

			Log.Info("Restoring messages from journal.");
			var restored = this.messages.Restore(info, this.journalDisruptor, this.repository);
			if (!restored)
				return this.started = false;

			Log.InfoFormat("Taking snapshots of hydratables at sequence {0}", info.JournaledSequence);
			this.snapshots.SaveSnapshot(this.repository, this.snapshotDisruptor.RingBuffer, info);

			Log.Info("Starting primary transformation disruptor.");
			this.transformationDisruptor = this.disruptors.CreateTransformationDisruptor(this.repository, info);
			this.transformationDisruptor.Start();

			GC.Collect(2, GCCollectionMode.Forced);

			Log.Info("Attempting to start message listener.");
			this.clock = this.timeout.CreateSystemClock(this.transformationDisruptor.RingBuffer);
			this.clock.Start();
			this.listener = this.messaging.CreateMessageListener(this.transformationDisruptor.RingBuffer);
			this.listener.Start();

			Log.Info("Bootstrap process complete; listening for incoming messages.");
			return this.started = true;
		}

		internal Bootstrapper(
			IRepository repository,
			DisruptorFactory disruptors,
			SnapshotBootstrapper snapshots,
			MessageBootstrapper messages,
			TimeoutFactory timeout,
			MessagingFactory messaging)
		{
			if (repository == null) throw new ArgumentNullException("repository");
			if (disruptors == null) throw new ArgumentNullException("disruptors");
			if (snapshots == null) throw new ArgumentNullException("snapshots");
			if (messages == null) throw new ArgumentNullException("messages");
			if (timeout == null) throw new ArgumentNullException("timeout");
			if (messaging == null) throw new ArgumentNullException("messaging");

			this.repository = repository;
			this.disruptors = disruptors;
			this.snapshots = snapshots;
			this.messages = messages;
			this.timeout = timeout;
			this.messaging = messaging;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || !this.started)
				return;

			Log.Info("Shutting down message listener.");
			this.listener = this.listener.TryDispose();
			this.clock = this.clock.TryDispose();

			Log.Info("Shutting down transformation disruptor.");
			this.transformationDisruptor = this.transformationDisruptor.TryDispose();

			Log.Info("Shutting down snapshot disruptor.");
			this.snapshotDisruptor = this.snapshotDisruptor.TryDispose();

			Log.Info("Shutting down journal disruptor.");
			this.journalDisruptor = this.journalDisruptor.TryDispose();
			
			this.started = false;
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(Bootstrapper));
		private readonly IRepository repository;
		private readonly DisruptorFactory disruptors;
		private readonly SnapshotBootstrapper snapshots;
		private readonly MessageBootstrapper messages;
		private readonly TimeoutFactory timeout;
		private readonly MessagingFactory messaging;
		private IDisruptor<JournalItem> journalDisruptor;
		private IDisruptor<SnapshotItem> snapshotDisruptor;
		private IDisruptor<TransformationItem> transformationDisruptor;
		private MessageListener listener;
		private SystemClock clock;
		private bool started;
	}
}