namespace SampleApplication
{
	using System;
	using System.ComponentModel;

	[Description("json:underscore")]
	public class FizzBuzzProjection
	{
		public Guid StreamId { get; set; }
		public string Message { get; set; }
		public int Value { get; set; }
	}
}