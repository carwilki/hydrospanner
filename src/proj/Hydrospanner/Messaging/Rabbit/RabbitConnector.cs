﻿namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Collections.Generic;
	using System.Net.Security;
	using System.Security.Authentication;
	using RabbitMQ.Client;

	internal class RabbitConnector : IDisposable
	{
		public virtual IModel OpenChannel()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(RabbitConnector).Name);

			return this.TryOpenChannel();
		}
		private IModel TryOpenChannel()
		{
			try
			{
				var conn = this.Connect();
				return conn == null ? null : conn.CreateModel();
			}
			catch
			{
				this.Disconnect();
				return null;
			}
		}
		private IConnection Connect()
		{
			lock (this.sync)
			{
				var local = this.connection;
				if (local == null)
					this.connection = local = this.factory.CreateConnection();

				return local;
			}
		}
		private void Disconnect()
		{
			lock (this.sync)
			{
				var local = this.connection;
				this.connection = null;
				if (local != null)
					local.TryDispose();
			}
		}

		public RabbitConnector(Uri address) : this(address, new ConnectionFactory())
		{
		}
		public RabbitConnector(Uri address, ConnectionFactory factory)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			this.factory = factory;
			factory.HostName = address.Host;
			factory.Port = address.Port;

			var credentials = ParseUserInfo(address.UserInfo);
			factory.UserName = credentials.Key;
			factory.Password = credentials.Value;

			if (SecureConnection != address.Scheme)
				return;

			var accepted = address.Query.Contains(IgnoreIssuer) ? SslPolicyErrors.RemoteCertificateNameMismatch : SslPolicyErrors.None;
			factory.Ssl = new SslOption(address.Host)
			{
				Enabled = true,
				Version = SslProtocols.Tls,
				AcceptablePolicyErrors = accepted,
			};
		}
		protected RabbitConnector()
		{
		}
		private static KeyValuePair<string, string> ParseUserInfo(string credentials)
		{
			if (string.IsNullOrWhiteSpace(credentials))
				return new KeyValuePair<string, string>(Guest, Guest);

			var split = credentials.Split(":".ToCharArray());
			if (split.Length == 1)
				return new KeyValuePair<string, string>(split[0], Guest);

			return new KeyValuePair<string, string>(split[0], split[1]);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this.disposed)
				return;

			this.disposed = true;
			this.Disconnect();
		}

		private const string IgnoreIssuer = "ignore-issuer=true";
		private const string SecureConnection = "amqps";
		private const string Guest = "guest";
		private readonly object sync = new object();
		private readonly ConnectionFactory factory;
		private IConnection connection;
		private bool disposed;
	}
}