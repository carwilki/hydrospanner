#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Serialization
{
	using Machine.Specifications;

	[Subject(typeof(UnderscoreNormalizer))]
	public class when_resolving_a_property_name
	{
		public class when_the_input_is_null
		{
			Establish context = () =>
				input = null;

			It should_return_null = () =>
				result.ShouldBeNull();
		}

		public class when_the_input_is_empty
		{
			Establish context = () =>
				input = string.Empty;

			It should_return_empty = () =>
				result.ShouldBeEmpty();
		}

		public class when_the_input_is_all_lowercase
		{
			Establish context = () =>
				input = "lowercase";

			It should_return_the_input_unchanged = () =>
				result.ShouldEqual(input);
		}

		public class when_the_input_is_all_uppercase
		{
			Establish context = () =>
				input = "UPPERCASE";

			It should_return_the_input_as_lowercase = () =>
				result.ShouldEqual("uppercase");
		}

		public class when_the_input_is_pascal_cased
		{
			Establish context = () =>
				input = "PascalCasedInput";

			It should_return_split_the_words_and_put_underscores_between_them = () =>
				result.ShouldEqual("pascal_cased_input");
		}

		public class when_the_input_is_a_pascal_cased_proper_noun_with_sequential_capital_letters
		{
			Establish context = () =>
				input = "IPAddress";

			It should_return_split_the_words_and_put_underscores_between_them = () =>
				result.ShouldEqual("ip_address");
		}

		public class when_the_input_is_camel_cased_proper_noun
		{
			Establish context = () =>
				input = "eBayStore";

			It should_return_split_the_words_and_put_underscores_between_them = () =>
				result.ShouldEqual("ebay_store");
		}

		public class when_the_input_starts_with_multiple_lowercase_letters
		{
			Establish context = () =>
				input = "trueNORTHMedia";

			It should_split_after_the_first_uppercase_set_of_letters = () =>
				result.ShouldEqual("truenorth_media");
		}

		public class when_the_input_contains_a_number
		{
			Establish context = () =>
				input = "Channel5Media";

			It should_split_after_the_number = () =>
				result.ShouldEqual("channel5_media");
		}

		// TODO: spaces
		Establish context = () =>
			normalizer = new UnderscoreNormalizer();

		Because of = () =>
			result = normalizer.Normalize(input);

		static UnderscoreNormalizer normalizer;
		static string input;
		static string result;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414