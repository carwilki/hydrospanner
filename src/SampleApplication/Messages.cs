namespace SampleApplication
{
	using System;

	public class FizzEvent
	{
		public int Value { get; set; }
	}

	public class BuzzEvent
	{
		public int Value { get; set; }
	}

	public class FizzBuzzEvent
	{
		public int Value { get; set; }
	}

	public class CountEvent
	{
		public int Value { get; set; }
	}

	public class CountCommand
	{
		public Guid MessageId { get; set; }
		public int Value { get; set; }
	}
}