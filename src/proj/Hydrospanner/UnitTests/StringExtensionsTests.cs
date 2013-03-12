#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.UnitTests
{
	using Machine.Specifications;

	[Subject(typeof(StringExtensions))]
	public class when_formatting_a_string
	{
		It should_function_like_the_string_format_method = () =>
			"{0}-{1}".FormatWith(1, 2).ShouldEqual("1-2");
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
