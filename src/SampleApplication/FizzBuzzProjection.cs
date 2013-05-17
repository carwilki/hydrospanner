namespace SampleApplication
{
	using System;
	using System.ComponentModel;

	[Description("json:underscore")]
	public class FizzBuzzProjection : ICloneable
	{
		public string Message { get; set; }
		public int Value { get; set; }

		public object Clone()
		{
			return new FizzBuzzProjection
			{
				Message = this.Message,
				Value = this.Value
			};
		}
	}
}