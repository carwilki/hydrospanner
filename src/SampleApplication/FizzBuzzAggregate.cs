namespace SampleApplication
{
	public class FizzBuzzAggregate
	{
		public FizzBuzzAggregate(int memento = 0)
		{
			this.Value = memento;
		}

		public int Value { get; private set; }

		public object Increment(int value)
		{
			if (value % 3 == 0 && value % 5 == 0)
				return new FizzBuzzEvent { Value = value };

			if (value % 5 == 0)
				return new BuzzEvent { Value = value };

			if (value % 3 == 0)
				return new FizzEvent { Value = value };

			return new CountEvent { Value = value };
		}
		public void Apply(int value)
		{
			this.Value = value;
		}
	}
}