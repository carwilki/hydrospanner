#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Net.Security;
	using System.Security.Authentication;
	using Machine.Specifications;
	using NSubstitute;
	using RabbitMQ.Client;

	[Subject(typeof(RabbitConnector))]
	public class when_connecting_to_rabbitmq
	{
		public class when_no_server_address_is_provided
		{
			Because of = () =>
				Try(() => new RabbitConnector(null));

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ArgumentNullException>();
		}

		public class when_specifying_an_broker_address
		{
			Establish context = () =>
			{
				var address = new Uri("amqp://domain.com:1234/");
				connector = new RabbitConnector(address, factory);
			};

			It should_use_the_address_from_the_url_provided = () =>
			{
				factory.UserName.ShouldEqual("guest");
				factory.Password.ShouldEqual("guest");
				factory.HostName.ShouldEqual("domain.com");
				factory.Port.ShouldEqual(1234);
			};
		}

		public class when_specifying_an_broker_address_with_credentials
		{
			Establish context = () =>
			{
				var address = new Uri("amqp://username:password@domain.com:1234/");
				connector = new RabbitConnector(address, factory);
			};

			It should_use_the_address_from_the_url_provided = () =>
			{
				factory.UserName.ShouldEqual("username");
				factory.Password.ShouldEqual("password");
				factory.HostName.ShouldEqual("domain.com");
				factory.Port.ShouldEqual(1234);
			};
		}

		public class when_specifying_a_secure_broker_address
		{
			Establish context = () =>
			{
				var address = new Uri("amqps://domain.com:7890/");
				connector = new RabbitConnector(address, factory);
			};

			It should_connect_using_a_secure_connection = () =>
			{
				factory.Ssl.Enabled.ShouldBeTrue();
				factory.Ssl.ServerName.ShouldEqual("domain.com");
				factory.Ssl.Version.ShouldEqual(SslProtocols.Tls);
				factory.Ssl.AcceptablePolicyErrors.ShouldEqual(SslPolicyErrors.None);
				factory.Port.ShouldEqual(7890);
			};
		}

		public class when_specifying_to_ignore_server_certificate_errors
		{
			Establish context = () =>
			{
				var address = new Uri("amqps://domain.com:7890/?ignore-issuer=true");
				connector = new RabbitConnector(address, factory);
			};

			It should_connect_using_a_secure_connection = () =>
			{
				factory.Ssl.Enabled.ShouldBeTrue();
				factory.Ssl.Version.ShouldEqual(SslProtocols.Tls);
				factory.Ssl.ServerName.ShouldEqual("domain.com");
				factory.Ssl.AcceptablePolicyErrors.ShouldEqual(SslPolicyErrors.RemoteCertificateNameMismatch);
				factory.Port.ShouldEqual(7890);
			};
		}

		public class when_opening_a_channel_for_the_first_time
		{
			Because of = () =>
				openedChannel = connector.OpenChannel();

			It should_connect_to_the_broker = () =>
				factory.Received(1).CreateConnection();

			It should_open_a_channel_with_the_connection = () =>
				connection.Received(1).CreateModel();

			It should_return_a_new_channel = () =>
				openedChannel.ShouldNotBeNull();
		}

		public class when_opening_additional_channels
		{
			Establish context = () =>
			{
				connection.CreateModel().Returns(channel1, Substitute.For<IModel>());

				openedChannel = connector.OpenChannel();
			};

			Because of = () =>
				channel2 = connector.OpenChannel();

			It should_NOT_connect_to_the_broker_again = () =>
				factory.Received(1).CreateConnection();

			It should_open_a_channel_with_the_connection = () =>
				connection.Received(2).CreateModel();

			It should_return_a_new_channel = () =>
				openedChannel.ShouldNotBeNull();

			It should_return_a_unique_channel = () =>
				openedChannel.ShouldNotEqual(channel2);

			static IModel channel2;
		}

		public class when_connecting_to_the_broker_fails
		{
			Establish context = () =>
				factory.CreateConnection().Returns(x => { throw new Exception(); });

			Because of = () =>
				openedChannel = connector.OpenChannel();

			It should_not_return_a_channel = () =>
				openedChannel.ShouldBeNull();

			It should_not_throw_an_exception = () =>
				thrown.ShouldBeNull();
		}

		public class when_opening_a_channel_fails
		{
			Establish context = () =>
				connection.CreateModel().Returns(x => { throw new Exception(); });

			Because of = () =>
				openedChannel = connector.OpenChannel();

			It should_not_return_a_channel = () =>
				openedChannel.ShouldBeNull();

			It should_not_throw_an_exception = () =>
				thrown.ShouldBeNull();

			It should_dispose_the_underlying_connection = () =>
				connection.Received(1).Dispose();
		}

		public class when_opening_a_channel_after_a_connection_has_failed
		{
			Establish context = () =>
			{
				connection2 = Substitute.For<IConnection>();
				factory.CreateConnection().Returns(connection, connection2);
				connection.CreateModel().Returns(x => { throw new Exception(); });
				connection2.CreateModel().Returns(channel1);

				connector.OpenChannel(); // fails
			};

			Because of = () =>
				openedChannel = connector.OpenChannel(); // re-attempt

			It should_open_a_new_connection_to_the_broker = () =>
				factory.Received(2).CreateConnection();

			It should_return_a_new_channel = () =>
				openedChannel.ShouldNotBeNull();

			static IConnection connection2;
		}

		public class when_disposing_after_a_channel_has_been_opened
		{
			Establish context = () =>
				connector.OpenChannel();

			Because of = () =>
				connector.Dispose();

			It should_dispose_the_underlying_connection = () =>
				connection.Received(1).Dispose();
		}

		public class when_disposing_multiple_times_after_a_channel_has_been_opened
		{
			Establish context = () =>
			{
				connector.OpenChannel();
				connector.Dispose();
			};

			Because of = () =>
				connector.Dispose();

			It should_dispose_the_underlying_connection = () =>
				connection.Received(1).Dispose();
		}

		public class when_disposing_a_connection_throws_an_exception
		{
			Establish context = () =>
			{
				connection.When(x => x.Dispose()).Do(x => { throw new Exception(); });
				connector.OpenChannel();
			};

			Because of = () =>
				connector.Dispose();

			It should_NOT_throw_an_exception = () =>
				thrown.ShouldBeNull();
		}

		public class when_open_a_channel_after_disposal
		{
			Establish context = () =>
				connector.Dispose();

			Because of = () =>
				Try(() => connector.OpenChannel());

			It should_throw_an_exception = () =>
				thrown.ShouldBeOfType<ObjectDisposedException>();
		}

		static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		Establish context = () =>
		{
			factory = Substitute.For<ConnectionFactory>();
			connection = Substitute.For<IConnection>();
			channel1 = Substitute.For<IModel>();

			factory.CreateConnection().Returns(connection);
			connection.CreateModel().Returns(channel1);

			address = new Uri("ampq://localhost:5671/");
			connector = new RabbitConnector(address, factory);
		};

		Cleanup after = () =>
			thrown = null;

		static Uri address;
		static Exception thrown;
		static ConnectionFactory factory;
		static RabbitConnector connector;
		static IConnection connection;
		private static IModel channel1;
		static IModel openedChannel;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169