namespace Hydrospanner.Serialization
{
	using System;
	using System.ComponentModel;
	using System.Reflection;
	using System.Runtime.Serialization;

	internal static class UnderscoreExtensions
	{
		public static bool HasJsonUnderscoreAttribute(this Type type)
		{
			if (type == null)
				return false;

			var descriptions = (DescriptionAttribute[])type.GetCustomAttributes(typeof(DescriptionAttribute), false);
			for (var i = 0; i < descriptions.Length; i++)
				if (descriptions[i].Description == "json:underscore")
					return true;

			return false;
		}
		public static string ParseContractName(this MemberInfo member)
		{
			var attributes = (DataMemberAttribute[])member.GetCustomAttributes(typeof(DataMemberAttribute), false);
			return attributes.Length == 0 ? null : attributes[0].Name;
		}
	}
}