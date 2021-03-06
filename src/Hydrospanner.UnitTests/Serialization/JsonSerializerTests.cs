﻿#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Text;
	using Machine.Specifications;

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_and_deserializing_objects
	{
		public class during_serialization
		{
			public class when_the_object_is_null
			{
				It should_return_null = () =>
					serializer.Serialize(null).ShouldEqual(null);
			}

			public class when_the_object_is_NOT_null
			{
				It should_serialize_primatives = () =>
				{
					serializer.Serialize(1).ShouldBeOfType<byte[]>();
					serializer.Serialize(1).Length.ShouldBeGreaterThan(0);
				};

				It should_serialize_complex_types = () =>
					serializer.Deserialize<MyComplexType>(serializer.Serialize(Thing)).ShouldBeLike(Thing);

				static readonly DateTime Now = DateTime.UtcNow;
				static readonly DateTime Instant = new DateTime(Now.Year, Now.Month, Now.Day, Now.Hour, Now.Minute, Now.Second, DateTimeKind.Utc);
				static readonly MyComplexType Thing = new MyComplexType
				{
					First = 1,
					Second = 2,
					Third = 3,
					Fourth = 4,
					Fifth = 5,
					Sixth = Guid.NewGuid(),
					Seventh = "Hello, World!",
					Eighth = new Uri("http://www.google.com/"),
					Ninth = Instant,
					Tenth = Values.First
				};
			}

			public class when_serializing_a_dictionary
			{
				Establish context = () =>
					payload = new Dictionary<string, string> { { "Hello", "World" } };

				Because of = () =>
					json = Encoding.UTF8.GetString(serializer.Serialize(payload));

				It should_not_modify_the_key_names = () =>
				{
					var parsed = json
						.Replace("\r\n", string.Empty)
						.Replace(" ", string.Empty);

					parsed.ShouldEqual("{\"Hello\":\"World\"}");
				};

				static Dictionary<string, string> payload; 
				static string json;
			}
			
			public class when_serializing_a_complex_dictionary
			{
				Establish context = () =>
					payload = new Dictionary<string, MyComplexType> { { "MyKey", new MyComplexType { Second = 1 } } };

				Because of = () =>
					json = Encoding.UTF8.GetString(serializer.Serialize(payload));

				It should_modify_the_key_names_of_the_value_items = () =>
				{
					var parsed = json
						.Replace("\r\n", string.Empty)
						.Replace(" ", string.Empty);

					parsed.ShouldEqual("{\"MyKey\":{\"Second\":1}}");
				};

				static Dictionary<string, MyComplexType> payload;
				static string json;
			}

			public class when_serialization_raises_an_exception
			{
				Establish context = () =>
				{
					thing = new Cycle();
					thing.Inner = thing;
				};

				Because of = () =>
					exception = Catch.Exception(() => serializer.Serialize(thing));

				It should_raise_an_exception = () =>
					exception.ShouldBeOfType<SerializationException>();

				class Cycle
				{
// ReSharper disable UnusedAutoPropertyAccessor.Local
					public Cycle Inner { get; set; }
// ReSharper restore UnusedAutoPropertyAccessor.Local
				}

				static Cycle thing;
				static Exception exception;
			}
		}

		public class during_deserializtion
		{
			public class when_the_serialized_content_is_null
			{
				It should_return_null = () =>
					serializer.Deserialize(null, null).ShouldBeNull();
			}

			public class when_the_serialized_content_is_empty
			{
				It should_return_null = () =>
					serializer.Deserialize(new byte[0], null).ShouldBeNull();
			}

			public class when_the_serialized_content_is_deserialized
			{
				It should_return_the_expected_object = () =>
					serializer.Deserialize(Json, "System.Int32").ShouldEqual(1);

				static readonly byte[] Json = Encoding.UTF8.GetBytes("1");
			}

			public class when_the_typename_is_NOT_found
			{
				Because of = () =>
					raised = Catch.Exception(() => serializer.Deserialize(new byte[] { 0 }, "invalid-type"));

				It should_throw_an_exception = () =>
					raised.ShouldBeOfType<SerializationException>();

				It should_throw_an_exception_because_of_type_name_resolution_failure = () =>
					raised.Message.ShouldEqual("Type 'invalid-type' not found.");

				static Exception raised;
			}

			public class when_deserialization_fails_because_of_malformed_content
			{
				It should_throw_an_exception = () =>
					Catch.Exception(() => serializer.Deserialize(Malformed, "System.Int32")).ShouldBeOfType<SerializationException>();

				static readonly byte[] Malformed = Encoding.UTF8.GetBytes("Not a json object.");
			}
		}

		public class during_genericized_deserialization
		{
			public class when_the_serialized_content_is_null
			{
				It should_return_the_default_value_of_the_type_supplied = () =>
					serializer.Deserialize<int>(null).ShouldEqual(default(int));
			}

			public class when_the_serialized_content_is_empty
			{
				It should_return_the_default_value_of_the_type_supplied = () =>
					serializer.Deserialize<int>(new byte[0]).ShouldEqual(default(int));
			}

			public class when_the_serialized_content_is_deserialized
			{
				It should_return_the_expected_object = () =>
					serializer.Deserialize<int>(Json).ShouldEqual(1);

				static readonly byte[] Json = Encoding.UTF8.GetBytes("1");
			}

			public class when_deserialization_fails_because_of_malformed_content
			{
				It should_throw_an_exception = () =>
					Catch.Exception(() => serializer.Deserialize<int>(Malformed)).ShouldBeOfType<SerializationException>();

				static readonly byte[] Malformed = Encoding.UTF8.GetBytes("Not a json object.");
			}
		}

		Establish context = () =>
			serializer = new JsonSerializer();

		static JsonSerializer serializer;
	}

	[DataContract]
	[Serializable]
	public class MyComplexType
	{
		[DataMember(Name = "alternate_name")]
		public int First { get; set; }
		[DataMember]
		public long Second { get; set; }
		[DataMember]
		public decimal Third { get; set; }
		[DataMember]
		public ushort Fourth { get; set; }
		[DataMember]
		public byte Fifth { get; set; }
		[DataMember]
		public Guid Sixth { get; set; }
		[DataMember]
		public string Seventh { get; set; }
		[DataMember]
		public Uri Eighth { get; set; }
		[DataMember]
		public DateTime Ninth { get; set; }
		[DataMember]
		public Values Tenth { get; set; }
	}

	public enum Values
	{
		First,
		Second,
		Third,
		Fourth
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
