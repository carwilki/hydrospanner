namespace SampleApplication.Starter
{
	using System;
	using System.Collections;
	using System.Configuration;
	using System.Globalization;
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
			var properties = channel.CreateBasicProperties();
			properties.SetPersistent(false);
			properties.Headers = new Hashtable();
			
			var count = DetermineBoundsOfMessageGeneration();

			for (var i = count.Item1; i < count.Item2 + 1; i++)
			{
				var message = new CountCommand { Value = i, MessageId = Guid.NewGuid() };
				var json = JsonConvert.SerializeObject(message, Formatting.Indented, Settings);
				var payload = DefaultEncoding.GetBytes(json);

				properties.MessageId = Guid.NewGuid().ToString();
				properties.Type = message.GetType().AssemblyQualifiedName;

				channel.BasicPublish(string.Empty, QueueName, properties, payload);
			}
		}

		static Tuple<int, int> DetermineBoundsOfMessageGeneration()
		{
			Console.Write("Enter starting number: (1) ");
			var start = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(start) || int.Parse(start) < 1)
				start = "1";

			Console.Write("Enter ending number: (100000) ");
			var end = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(end) || int.Parse(end) < 1)
				end = "100000";

			Console.WriteLine("\n\nSending messages numbered {0} through {1}...", start, end);

			return Tuple.Create(int.Parse(start), int.Parse(end));
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