namespace SampleApplication.Starter
{
	using System;
	using System.Collections;
	using System.Configuration;
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;
	using RabbitMQ.Client;

	internal static class Program
	{
		private static void Main()
		{
			var factory = new ConnectionFactory { Endpoint = new AmqpTcpEndpoint(ServerAddress) };
			using (var connection = factory.CreateConnection())
			using (var channel = connection.CreateModel())
				SendMessages(channel);

			Console.WriteLine("Done");
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
			var random = new Random();

			var properties = channel.CreateBasicProperties();
			properties.SetPersistent(false);
			properties.Headers = new Hashtable();
			
			var count = DetermineBoundsOfMessageGeneration();

			for (var i = 0; i < count.Item1 + 1; i++)
			{
				var streamId = Guid.NewGuid();
				for (var x = 0; x < random.Next(1, count.Item2); x++)
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
			var json = JsonConvert.SerializeObject(message, Formatting.Indented, Settings);
			var payload = DefaultEncoding.GetBytes(json);

			properties.MessageId = messageId.ToString();
			properties.Type = message.GetType().AssemblyQualifiedName;

			channel.BasicPublish(string.Empty, QueueName, properties, payload);
		}

		static Tuple<int, int> DetermineBoundsOfMessageGeneration()
		{
			Console.Write("How many streams: (1000) ");
			var streams = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(streams) || int.Parse(streams) < 1)
				streams = "1000";

			Console.Write("Max commands per stream: (10) ");
			var commands = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(commands) || int.Parse(commands) < 1)
				commands = "10";

			Console.WriteLine("\n\nSending up to {0} commands for {1} streams...", commands, streams);

			return Tuple.Create(int.Parse(streams), int.Parse(commands));
		}

		private static readonly Uri ServerAddress = new Uri(ConfigurationManager.AppSettings["rabbit-server"]);
		private static readonly string QueueName = ConfigurationManager.AppSettings["queue-name"];
		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.None,
			TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
			DefaultValueHandling = DefaultValueHandling.Ignore,
			NullValueHandling = NullValueHandling.Ignore,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			DateTimeZoneHandling = DateTimeZoneHandling.Utc,
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateParseHandling = DateParseHandling.DateTime,
			Converters = { new StringEnumConverter() }
		};
	}
}