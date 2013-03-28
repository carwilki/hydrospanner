namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public class ConventionRoutingTable : IRoutingTable
    {
        public IEnumerable<string> Lookup(object message, Dictionary<string, string> headers)
        {
            return null;
        }
        public IHydratable Create(object message, Dictionary<string, string> headers)
        {
            return null;
        }
        public IHydratable Create(object memento)
        {
            return null;
        }

		public ConventionRoutingTable()
		{
		}
        public ConventionRoutingTable(params Assembly[] assemblies) : this(assemblies.SelectMany(x => x.GetTypes()))
        {
        }
		public ConventionRoutingTable(params Type[] types) : this((IEnumerable<Type>)types)
		{
		}
		public ConventionRoutingTable(IEnumerable<Type> types)
		{

			// TODO: Find methods with the following signatures:
			// 1. "static Key(T, Dictionary<string, string>):string"
			// 2. "static Create(T, Dictionary<string, string>):IHydratable"
			// 3. "static Create(T):IHydratable"

			// take each of the methods found and register it with the corresponding dictionary
			// then during the lookup/create phases, invoke those various methods

			foreach (var type in types ?? new Type[0])
				this.RegisterMementoFactory(type);
		}
		private void RegisterMementoFactory(Type type)
		{
			var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
			foreach (var method in methods)
				RegisterMementoFactory(method);
		}
		private void RegisterMementoFactory(MethodInfo method)
		{
			if (method.IsGenericMethod)
				return;

			if (method.Name != FactoryMethodName)
				return;

			if (!typeof(IHydratable).IsAssignableFrom(method.ReturnType))
				return;

			var parameters = method.GetParameters(); // TODO: fork at this point and register the factory from a memento or message
			if (parameters.Length != 1)
				return;

			var mementoType = parameters[0].ParameterType;
			if (mementoType == typeof(object))
				return;

			this.mementos[mementoType] = MakeFactoryMethod(method, mementoType);
		}

		private static Func<object, IHydratable> MakeFactoryMethod(MethodInfo method, Type generic)
		{
			return null;
		}

		private const string FactoryMethodName = "Create";
		private readonly Dictionary<Type, Func<object, IHydratable>> mementos = new Dictionary<Type, Func<object, IHydratable>>();
    }
}