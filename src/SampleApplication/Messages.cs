namespace SampleApplication
{
	using System;
	using System.ComponentModel;

	[Description("json:underscore")]
	public class FizzEvent
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}

	[Description("json:underscore")]
	public class BuzzEvent
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}

	[Description("json:underscore")]
	public class FizzBuzzEvent
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}

	[Description("json:underscore")]
	public class CountEvent
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}

	[Description("json:underscore")]
	public class CountCommand
	{
		public Guid StreamId { get; set; }
		public Guid MessageId { get; set; }
		public int Value { get; set; }
	}
}