﻿namespace SampleApplication
{
	using System;
	using System.Configuration;
	using System.Reflection;
	using System.Threading;
	using Hydrospanner;
	using Hydrospanner.Wireup;

	internal class Program
	{
		static readonly short NodeId = short.Parse(ConfigurationManager.AppSettings["NodeId"]);
		static readonly string MessageLogConnectionName = ConfigurationManager.ConnectionStrings["MessageLog"].Name;
		static readonly string PublicSnapshotConnectionName = ConfigurationManager.ConnectionStrings["PublicSnapshots"].Name;
		static readonly string BrokerAddress = ConfigurationManager.AppSettings["BrokerAddress"];
		static readonly string SourceQueue = ConfigurationManager.AppSettings["SourceQueue"];
		static readonly string SnapshotPath = ConfigurationManager.AppSettings["SnapshotPath"];
		static readonly int SnapshotGeneration = int.Parse(ConfigurationManager.AppSettings[SnapshotGeneration]);
		static readonly int DuplicateWindow = int.Parse(ConfigurationManager.AppSettings["DuplicateWindow"]);

		private static void Main()
		{
			var repository = new DefaultRepository(new ConventionRoutingTable(Assembly.GetExecutingAssembly()));
			var messagingFactory = new MessagingFactory(NodeId, new Uri(BrokerAddress), SourceQueue);
			var persistenceFactory = new PersistenceFactory(MessageLogConnectionName, DuplicateWindow);
			var persistenceBootstrapper = new PersistenceBootstrapper(persistenceFactory);
			var info = persistenceBootstrapper.Restore();
			var messageStore = persistenceFactory.CreateMessageStore(info.SerializedTypes);
			var snapshotFactory = new SnapshotFactory(SnapshotGeneration, SnapshotPath, PublicSnapshotConnectionName);
			var disruptorFactory = new DisruptorFactory(messagingFactory, persistenceFactory, snapshotFactory);
			var snapshotBootstrapper = new SnapshotBootstrapper(snapshotFactory, disruptorFactory);
			var messageBootstrapper = new MessageBootstrapper(messageStore, disruptorFactory);
			
			var bootstrapper = new Bootstrapper(
				repository, 
				disruptorFactory, 
				persistenceBootstrapper, 
				snapshotBootstrapper, 
				messageBootstrapper, 
				messagingFactory);

			using (bootstrapper)
			{
				bootstrapper.Start(info);
				Thread.Sleep(1000);
			}
		}
	}
}