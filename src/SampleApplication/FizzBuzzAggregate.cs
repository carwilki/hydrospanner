namespace SampleApplication
{
	using System;

	public class FizzBuzzAggregate
	{
		public FizzBuzzAggregate(int memento)
		{
			this.Value = memento;
		}

		public FizzBuzzAggregate(Guid streamId)
		{
			this.streamId = streamId;
		}

		public int Value { get; private set; }

		public bool IsComplete { get { return this.Value == 15; } }

		public object Increment(int value)
		{
			if (value % 3 == 0 && value % 5 == 0)
				return new FizzBuzzEvent { StreamId = this.streamId, Value = value };

			if (value % 5 == 0)
				return new BuzzEvent { StreamId = this.streamId, Value = value };

			if (value % 3 == 0)
				return new FizzEvent { StreamId = this.streamId, Value = value };

			return new CountEvent { StreamId = this.streamId, Value = value };
		}
		public void Apply(int value)
		{
			this.Value = value;
		}

		private readonly Guid streamId;
	}
}