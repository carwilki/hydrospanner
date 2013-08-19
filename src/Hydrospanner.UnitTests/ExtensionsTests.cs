#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Machine.Specifications;

	[Subject(typeof(StringExtensions))]
	public class when_formatting_a_string
	{
		It should_function_like_the_string_format_method = () =>
			"{0}-{1}-{0}".FormatWith(1, 2).ShouldEqual("1-2-1");
	}

	[Subject(typeof(CollectionExtensions))]
	public class when_retrieving_values_from_dictionaries
	{
		Establish context = () =>
			stuff = new Dictionary<string, string> { { "Hello", "World" } };

		public class when_retrieving_the_value_or_an_appropriate_default
		{
			It should_provide_a_default_value_when_the_key_is_absent = () =>
				stuff.ValueOrDefault("blah").ShouldBeNull();

			It should_provide_the_stored_value_when_preset = () =>
				stuff.ValueOrDefault("Hello").ShouldEqual("World");
		}

		public class when_retrieving_the_value_and_adding_when_absent
		{
			It should_add_a_value_if_it_is_absent = () =>
				stuff.Add("See you", () => "later").ShouldEqual("later");

			It should_retreive_the_value_if_it_is_preset = () =>
				stuff.Add("Hello", () => "asdfasdf").ShouldEqual("World");
		}

		static Dictionary<string, string> stuff;
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
#pragma warning restore 169, 414
