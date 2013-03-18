#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner
{
	using System;
	using System.Linq;
	using System.Text;
	using Machine.Specifications;

	[Subject(typeof(StringExtensions))]
	public class when_formatting_a_string
	{
		It should_function_like_the_string_format_method = () =>
			"{0}-{1}-{0}".FormatWith(1, 2).ShouldEqual("1-2-1");
	}

	[Subject(typeof(ByteConversionExtensions))]
	public class when_converting_byte_arrays_to_values
	{
		It should_not_slice_from_an_empty_array = () =>
			Catch.Exception(() => new byte[0].SliceString(0, 4)).ShouldBeOfType<ArgumentException>();

		It should_slice_integers_from_an_array = () =>
		{
			Stuff.SliceInt32(0).ShouldEqual(42);
			Stuff.SliceInt32(sizeof(int) + Text.Length).ShouldEqual(42);
		};

		It should_slice_a_string_from_the_middle_of_an_array = () =>
			Stuff.SliceString(4, Text.Length).ShouldEqual(Text);

		It should_slice_a_string_from_the_end_of_an_array = () =>
			Stuff.SliceString(sizeof(int) + Text.Length + sizeof(int)).ShouldEqual(Text);

		It should_convert_a_string_to_bytes = () =>
			Text.ToByteArray().ShouldBeLike(BinaryText);

		It should_convert_an_int32_to_bytes = () =>
			Integer.ToByteArray().ShouldBeLike(BinaryInteger);

		const int Integer = 42;
		static readonly byte[] BinaryInteger = BitConverter.GetBytes(Integer);
		const string Text = "Hello, World!";
		static readonly byte[] BinaryText = Encoding.UTF8.GetBytes(Text);
		static readonly byte[] Stuff = BinaryInteger
			.Concat(BinaryText)
			.Concat(BinaryInteger)
			.Concat(BinaryText)
			.ToArray();
	}

	
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
