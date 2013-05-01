namespace Hydrospanner.Serialization
{
	using System;
	using System.Reflection;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Serialization;

	internal class UnderscoreContractResolver : DefaultContractResolver
	{
		public override JsonContract ResolveContract(Type type)
		{
			if (type.HasJsonUnderscoreAttribute())
				return base.ResolveContract(type);

			return this.resolver.ResolveContract(type);
		}
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var property = base.CreateProperty(member, memberSerialization);
			property.PropertyName = member.ParseContractName() ?? this.normalizer.Normalize(property.PropertyName);
			return property;
		}

		private readonly DefaultContractResolver resolver = new DefaultContractResolver();
		private readonly UnderscoreNormalizer normalizer = new UnderscoreNormalizer();
	}
}