namespace SampleApplication
{
	using System;
	using System.Collections.Generic;

	public class FizzBuzzAggregate
	{
		public List<object> PendingMessages { get; private set; } 
		public int Memento { get; private set; }

		public FizzBuzzAggregate(int memento)
		{
			this.PendingMessages = new List<object>();
			this.Memento = memento;
		}

		public bool IsComplete { get { return this.Memento == 15; } }

		public void Increment(Guid streamId, int value)
		{
			if (value % 3 == 0 && value % 5 == 0)
				this.PendingMessages.Add(new FizzBuzzEvent { StreamId = streamId, Value = value });
			else if (value % 5 == 0)
				this.PendingMessages.Add(new BuzzEvent { StreamId = streamId, Value = value });
			else if (value % 3 == 0)
				this.PendingMessages.Add(new FizzEvent { StreamId = streamId, Value = value });
			else
				this.PendingMessages.Add(new CountEvent { StreamId = streamId, Value = value });
		}
		public void Apply(FizzBuzzEvent message)
		{
			this.Memento = message.Value;
		}
		public void Apply(BuzzEvent message)
		{
			this.Memento = message.Value;
		}
		public void Apply(FizzEvent message)
		{
			this.Memento = message.Value;
		}
		public void Apply(CountEvent message)
		{
			this.Memento = message.Value;
		}
	}
}