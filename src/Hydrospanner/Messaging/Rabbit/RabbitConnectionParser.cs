namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Collections.Generic;
	using System.Net.Security;
	using System.Security.Authentication;
	using RabbitMQ.Client;

	public static class RabbitConnectionParser
	{
		public static ConnectionFactory Parse(Uri address)
		{
			var credentials = ParseUserInfo(address.UserInfo);
			return new ConnectionFactory
			{
				HostName = address.Host,
				Port = address.Port,
				UserName = credentials.Key,
				Password = credentials.Value,
				Ssl = ParseSsl(address)
			};
		}
		private static SslOption ParseSsl(Uri address)
		{
			if (SecureConnection != address.Scheme)
				return new SslOption();

			var accepted = address.Query.Contains(IgnoreIssuer) ? SslPolicyErrors.RemoteCertificateChainErrors : SslPolicyErrors.None;
			return new SslOption(address.Host)
			{
				Enabled = true,
				Version = SslProtocols.Tls,
				AcceptablePolicyErrors = accepted,
			};
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

		private const string IgnoreIssuer = "ignore-issuer=true";
		private const string SecureConnection = "amqps";
		private const string Guest = "guest";
	}
}