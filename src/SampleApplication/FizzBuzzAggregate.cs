namespace SampleApplication
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

		private void Append(object message, bool append)
		{
			if (append)
				this.Messages.Add(message);
		}

		public void Increment(int value)
		{
			if (value % 3 == 0 && value % 5 == 0)
				this.Apply(new FizzBuzzEvent { StreamId = this.streamId, Value = value }, true);
			else if (value % 5 == 0)
				this.Apply(new BuzzEvent { StreamId = this.streamId, Value = value }, true);
			else if (value % 3 == 0)
				this.Apply(new FizzEvent { StreamId = this.streamId, Value = value }, true);
			else
				this.Apply(new CountEvent { StreamId = this.streamId, Value = value }, true);
		}
		public void Apply(FizzBuzzEvent message, bool append = false)
		{
			this.Append(message, append);
			this.Value = message.Value;
		}
		public void Apply(BuzzEvent message, bool append = false)
		{
			this.Append(message, append);
			this.Value = message.Value;
		}
		public void Apply(FizzEvent message, bool append = false)
		{
			this.Append(message, append);
			this.Value = message.Value;
		}
		public void Apply(CountEvent message, bool append = false)
		{
			this.Append(message, append);
			this.Value = message.Value;
		}

		private readonly Guid streamId;
	}
}