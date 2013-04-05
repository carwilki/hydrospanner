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

		public object Increment(int value)
		{
			if (value % 3 == 0 && value % 5 == 0)
				return new FizzBuzzEvent { Value = value, StreamId = this.streamId };

			if (value % 5 == 0)
				return new BuzzEvent { Value = value, StreamId = this.streamId };

			if (value % 3 == 0)
				return new FizzEvent { Value = value, StreamId = this.streamId };

			return new CountEvent { Value = value, StreamId = this.streamId };
		}
		public void Apply(int value)
		{
			this.Value = value;
		}

		private readonly Guid streamId;
	}
}