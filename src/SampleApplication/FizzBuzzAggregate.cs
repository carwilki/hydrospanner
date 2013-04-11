﻿namespace SampleApplication
{
	using System;
	using System.Collections.Generic;

	public class FizzBuzzAggregate
	{
		public FizzBuzzAggregate(Guid streamId, int value) : this(streamId)
		{
			this.Value = value;
		}
		public FizzBuzzAggregate(Guid streamId)
		{
			this.streamId = streamId;
			this.Messages = new List<object>();
		}

		public int Value { get; private set; }
		public bool IsComplete { get { return this.Value == 15; } }
		public List<object> Messages { get; private set; }

		public void Increment(int value)
		{
			if (value % 3 == 0 && value % 5 == 0)
				this.Append(this.Apply, new FizzBuzzEvent { StreamId = this.streamId, Value = value });
			else if (value % 5 == 0)
				this.Append(this.Apply, new BuzzEvent { StreamId = this.streamId, Value = value });
			else if (value % 3 == 0)
				this.Append(this.Apply, new FizzEvent { StreamId = this.streamId, Value = value });
			else
				this.Append(this.Apply, new CountEvent { StreamId = this.streamId, Value = value });
		}
		private void Append<T>(Action<T> callback, T message)
		{
			this.Messages.Add(message);
			callback(message);
		}
		public void Apply(FizzBuzzEvent message)
		{
			this.Value = message.Value;
		}
		public void Apply(BuzzEvent message)
		{
			this.Value = message.Value;
		}
		public void Apply(FizzEvent message)
		{
			this.Value = message.Value;
		}
		public void Apply(CountEvent message)
		{
			this.Value = message.Value;
		}

		private readonly Guid streamId;
	}
}