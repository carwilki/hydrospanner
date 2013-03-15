﻿namespace Hydrospanner.Messaging
{
	using System;
	using System.Collections.Generic;

	internal struct MessageDelivery
	{
		public static readonly MessageDelivery Empty = new MessageDelivery();

		public bool Populated { get; private set; }
		public Guid MessageId { get; private set; }
		public byte[] Payload { get; private set; }
		public Dictionary<string, string> Headers { get; private set; }
		public string MessageType { get; private set; }
		public Action Acknowledge { get; private set; }

		public MessageDelivery(Guid messageId, byte[] payload, string type, Dictionary<string, string> headers, Action acknowledge) : this()
		{
			this.Populated = true;
			this.MessageId = messageId;
			this.Payload = payload;
			this.Headers = headers;
			this.MessageType = type;
			this.Acknowledge = acknowledge;
		}
	}
}