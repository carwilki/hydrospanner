namespace SampleApplication
{
	using System;

	public class FizzEvent
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}

	public class BuzzEvent
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}

	public class FizzBuzzEvent
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}

	public class CountEvent
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}

	public class CountCommand
	{
		public Guid StreamId { get; set; }
		public Guid MessageId { get; set; }
		public int Value { get; set; }
	}
}