#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.UnitTests
{
	using Machine.Specifications;

	[Subject(typeof(StringExtensions))]
	internal class when_formatting_a_string
	{
		It should_function_like_the_string_format_method = () =>
			"{0}-{1}-{0}".FormatWith(1, 2).ShouldEqual("1-2-1");
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
