#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner
{
	using System;
	using Machine.Specifications;

	[Subject(typeof(StringExtensions))]
	public class when_formatting_a_string
	{
		It should_function_like_the_string_format_method = () =>
			"{0}-{1}-{0}".FormatWith(1, 2).ShouldEqual("1-2-1");
	}

	[Subject(typeof(EnumerationExtensions))]
	public class when_testing_a_bitwise_enum_for_inclusion
	{
		It should_register_inclusion_appropriately = () =>
		{
			First.HasFlag(MyEnum.First).ShouldBeTrue(); // TODO: how about this? It's built in to .NET 4

			First.Includes(MyEnum.First).ShouldBeTrue();
			First.Includes(MyEnum.Second).ShouldBeFalse();
			First.Includes(MyEnum.Third).ShouldBeFalse();

			FirstAndSecond.Includes(MyEnum.First).ShouldBeTrue();
			FirstAndSecond.Includes(MyEnum.Second).ShouldBeTrue();
			FirstAndSecond.Includes(MyEnum.Third).ShouldBeFalse();

			All.Includes(MyEnum.First).ShouldBeTrue();
			All.Includes(MyEnum.Second).ShouldBeTrue();
			All.Includes(MyEnum.Third).ShouldBeTrue();
		};

		const MyEnum First = MyEnum.First;
		const MyEnum FirstAndSecond = MyEnum.First | MyEnum.Second;
		const MyEnum All = MyEnum.First | MyEnum.Second | MyEnum.Third;

		[Flags]
		enum MyEnum
		{
			First = 1,
			Second = 2,
			Third = 4
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
