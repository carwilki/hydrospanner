namespace TestSender
{
	using System;
	using System.Collections;
	using System.Configuration;
	using System.Globalization;
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Accounting.Events;
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
			{
				var properties = channel.CreateBasicProperties();
				properties.SetPersistent(false);
				properties.Headers = new Hashtable();

				var message = BuildMessage();
				var json = JsonConvert.SerializeObject(message, Formatting.Indented, Settings);
				var payload = DefaultEncoding.GetBytes(json);

				try
				{
					for (var i = 0; i < 1; i++)
					{
						properties.MessageId = (i + 1).ToString(CultureInfo.InvariantCulture);

						channel.BasicPublish(string.Empty, QueueName, properties, payload);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}

			Console.WriteLine("Done");
		}
		private static object BuildMessage()
		{
			return new AccountClosedEvent
			{
				AccountId = Guid.NewGuid(),
				Description = "Hello, World!",
				Dispatched = DateTime.UtcNow,
				MessageId = Guid.NewGuid(),
				Reason = CloseReason.Abuse,
				UserId = Guid.NewGuid(),
				Username = "test@test.com"
			};
		}

		private static readonly Uri ServerAddress = new Uri(ConfigurationManager.AppSettings["rabbit-server"]);
		private static readonly string QueueName = ConfigurationManager.AppSettings["queue-name"];

		private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
		private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All,
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