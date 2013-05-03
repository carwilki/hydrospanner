#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Text;
	using Machine.Specifications;

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_undecorated_structure
	{
		It should_retain_pascal_casing = () =>
			new Plain(42).ToJson().ShouldEqual("{'PascalCase':42}".Quote());
	}
	internal class Plain
	{
		public int PascalCase { get; set; }

		public Plain(int value)
		{
			this.PascalCase = value;
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_a_decorated_structure
	{
		It should_serialize_according_to_the_datacontract = () =>
			new Decorated(42).ToJson().ShouldEqual("{'PascalCase':42,'lowercase':42}".Quote());
	}
	[DataContract]
	internal class Decorated
	{
		[DataMember]
		public int PascalCase { get; set; }

		[DataMember(Name = "lowercase")]
		public int Lowercase { get; set; }

		public int SkipMeForSerialization { get; set; } // don't serialize

		public Decorated(int value)
		{
			this.PascalCase = value;
			this.Lowercase = value;
			this.SkipMeForSerialization = value;
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_underscore_decorated_structure
	{
		It should_serialize_with_underscore = () =>
			new UnderscorePlain(42).ToJson().ShouldEqual("{'underscore_casing':42}".Quote());
	}
	[Description("json:underscore")]
	internal class UnderscorePlain
	{
		public int UnderscoreCasing { get; set; }

		public UnderscorePlain(int value)
		{
			this.UnderscoreCasing = value;
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_underscore_and_datacontract_decorated_structure
	{
		It should_serialize_with_underscore_and_according_to_the_contract = () =>
			new UnderscoreDecorated(42).ToJson().ShouldEqual("{'underscore_casing':42,'MyCasing':42}".Quote());
	}
	[DataContract]
	[Description("json:underscore")]
	internal class UnderscoreDecorated
	{
		[DataMember]
		public int UnderscoreCasing { get; set; }

		[DataMember(Name = "MyCasing")]
		public int DataMemberCasing { get; set; }

		public int Forgotten { get; set; }

		public UnderscoreDecorated(int value)
		{
			this.UnderscoreCasing = value;
			this.DataMemberCasing = value;
			this.Forgotten = value;
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_undecorated_outer_and_inner_structure
	{
		It should_serialize_both_with_pascal_casing = () =>
			new PlainOuter().ToJson().ShouldEqual("{'InnerPlain':{'PascalCase':42}}".Quote());
	}
	internal class PlainOuter
	{
		public Plain InnerPlain { get; set; }

		public PlainOuter()
		{
			this.InnerPlain = new Plain(42);
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_undecorated_outer_but_underscore_decorated_inner_structure
	{
		It should_serialize_the_outer_with_pascal_and_inner_with_underscores = () =>
			new PlainOuter2().ToJson().ShouldEqual("{'UnderscoreInner':{'underscore_casing':42}}".Quote());
	}
	internal class PlainOuter2
	{
		public UnderscorePlain UnderscoreInner { get; set; }

		public PlainOuter2()
		{
			this.UnderscoreInner = new UnderscorePlain(42);
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_underscore_decorated_outer_but_undecorated_inner_structure
	{
		It should_serialize_the_outer_with_underscores_and_inner_with_pascal_case = () =>
			new UnderscoreOuter().ToJson().ShouldEqual("{'plain_inner':{'PascalCase':42}}".Quote());
	}
	[Description("json:underscore")]
	internal class UnderscoreOuter
	{
		public Plain PlainInner { get; set; }

		public UnderscoreOuter()
		{
			this.PlainInner = new Plain(42);
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_underscore_decorated_outer_and_inner_structure
	{
		It should_serialize_the_everything_with_inner_with_underscores = () =>
			new UnderscoreComplex().ToJson().ShouldEqual("{'underscore_inner':{'underscore_casing':42},'keys':{'Hello':'World!'}}".Quote());
	}
	[Description("json:underscore")]
	internal class UnderscoreComplex
	{
		public UnderscorePlain UnderscoreInner { get; set; }

		public Dictionary<string, string> Keys { get; set; }

		public UnderscoreComplex()
		{
			this.UnderscoreInner = new UnderscorePlain(42);
			this.Keys = new Dictionary<string, string> { { "Hello", "World!" } }; // dictionary keys should remain uppercase
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_undecorated_structure_and_enum
	{
		It should_serialize_to_pascal_case = () =>
			new UndecoratedEnumWrapper().ToJson().ShouldEqual("{'PlainEnum':'PascalCase'}".Quote());
	}
	public class UndecoratedEnumWrapper
	{
		public PlainEnum PlainEnum { get; set; }

		public UndecoratedEnumWrapper()
		{
			this.PlainEnum = PlainEnum.PascalCase;
		}
	}
	public enum PlainEnum
	{
		None,
		PascalCase
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_undecorated_structure_but_underscore_decorated_enum
	{
		It should_serialize_the_outer_with_pascal_and_inner_with_underscores = () =>
			new UndecoratedUnderscoreEnumWrapper().ToJson().ShouldEqual("{'DecoratedEnum':'underscore_casing'}".Quote());
	}
	public class UndecoratedUnderscoreEnumWrapper
	{
		public UnderscoreEnum DecoratedEnum { get; set; }

		public UndecoratedUnderscoreEnumWrapper()
		{
			this.DecoratedEnum = UnderscoreEnum.UnderscoreCasing;
		}
	}
	[Description("json:underscore")]
	public enum UnderscoreEnum
	{
		None,
		UnderscoreCasing,
		AnotherOption
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_underscore_decorated_structure_but_plain_enum
	{
		It should_serialize_the_outer_with_underscores_and_inner_with_pascal = () =>
			new UnderscoreWrapper().ToJson().ShouldEqual("{'underscore_casing':'PascalCase'}".Quote());
	}
	[Description("json:underscore")]
	public class UnderscoreWrapper
	{
		public PlainEnum UnderscoreCasing { get; set; }

		public UnderscoreWrapper()
		{
			this.UnderscoreCasing = PlainEnum.PascalCase;
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_an_underscore_decorated_structure_with_underscore_enum
	{
		It should_serialize_both_with_underscores = () =>
			new UnderscoreWrapper2().ToJson().ShouldEqual("{'underscore_casing':'underscore_casing'}".Quote());
	}
	[Description("json:underscore")]
	public class UnderscoreWrapper2
	{
		public UnderscoreEnum UnderscoreCasing { get; set; }

		public UnderscoreWrapper2()
		{
			this.UnderscoreCasing = UnderscoreEnum.UnderscoreCasing;
		}
	}

	[Subject(typeof(JsonSerializer))]
	public class when_deserializing_an_underscore_decorated_structure_with_underscore_enum
	{
		It should_deserialize_both_from_underscores = () =>
			"{'underscore_casing':'another_option'}".Quote().ToObject<UnderscoreWrapper2>().UnderscoreCasing.ShouldEqual(UnderscoreEnum.AnotherOption);

		static UnderscoreWrapper2 value;
	}

	public static class SerializationTestExtensions
	{
		public static string Quote(this string value)
		{
			return value.Replace("'", "\"");
		}
		public static string ToJson(this ISerializer serializer, object graph)
		{
			var serialized = serializer.Serialize(graph);
			return Encoding.UTF8.GetString(serialized).Replace(Environment.NewLine, string.Empty).Replace(" ", string.Empty);
		}
		public static T ToObject<T>(this ISerializer serializer, string json)
		{
			var bytes = Encoding.UTF8.GetBytes(json);
			return serializer.Deserialize<T>(bytes);
		}

		public static string ToJson(this object graph)
		{
			return new JsonSerializer().ToJson(graph);
		}
		public static T ToObject<T>(this string json)
		{
			return new JsonSerializer().ToObject<T>(json);
		}
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414