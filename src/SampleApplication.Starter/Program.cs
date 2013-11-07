namespace SampleApplication.Starter
{
	using System;
	using System.Collections;
	using System.Configuration;
	using Hydrospanner.Serialization;
	using RabbitMQ.Client;

	internal static class Program
	{
		private static void Main()
		{
		    string token = string.Empty;
		    do
		    {
		        var factory = new ConnectionFactory { Endpoint = new AmqpTcpEndpoint(ServerAddress) };
		        using (var connection = factory.CreateConnection()) using (var channel = connection.CreateModel()) SendMessages(channel);

		        Console.WriteLine("Done");

		        token = Console.ReadLine();
		    }
		    while (token != "quit");
		}

		static void SendMessages(IModel channel)
		{
			try
			{
				SendTheMessages(channel);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		static void SendTheMessages(IModel channel)
		{
			var properties = channel.CreateBasicProperties();
			properties.SetPersistent(false);
			properties.Headers = new Hashtable();
			
			var count = DetermineBoundsOfMessageGeneration();

			for (var i = 0; i < count.Item1; i++)
			{
				var streamId = Guid.NewGuid();
				for (var x = 0; x < count.Item2; x++)
					SendMessage(channel, streamId, x + 1, properties);
			}
		}

		static void SendMessage(IModel channel, Guid streamId, int value, IBasicProperties properties)
		{
			var messageId = Guid.NewGuid();
			var message = new CountCommand
			{
				StreamId = streamId,
				Value = value,
				MessageId = messageId
			};

			var payload = Serializer.Serialize(message);
			properties.MessageId = messageId.ToString();
			properties.Type = message.GetType().AssemblyQualifiedName;

			channel.BasicPublish(string.Empty, QueueName, properties, payload);
		}

		static Tuple<int, int> DetermineBoundsOfMessageGeneration()
		{
			const string DefaultStream = "1000";
			const string DefaultCommands = "20";

			Console.Write("How many streams: ({0}) ", DefaultStream);
			var streams = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(streams) || int.Parse(streams) < 1)
				streams = DefaultStream;

			Console.Write("Max commands per stream: ({0}) ", DefaultCommands);
			var commands = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(commands) || int.Parse(commands) < 1)
				commands = DefaultCommands;

			Console.WriteLine("\n\nSending up to {0} commands for {1} streams...", commands, streams);

			return Tuple.Create(int.Parse(streams), int.Parse(commands));
		}

		private static readonly Uri ServerAddress = new Uri(ConfigurationManager.AppSettings["rabbit-server"]);
		private static readonly string QueueName = ConfigurationManager.AppSettings["queue-name"];
		private static readonly ISerializer Serializer = new Hydrospanner.Serialization.JsonSerializer();
	}
}