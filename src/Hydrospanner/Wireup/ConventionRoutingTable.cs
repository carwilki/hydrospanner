namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public class ConventionRoutingTable : IRoutingTable
	{
		public IEnumerable<HydrationInfo> Lookup(object message, Dictionary<string, string> headers)
		{
			throw new NotSupportedException();
		}
		public IHydratable Create(object memento)
		{
			if (memento == null)
				return null;

			return this.mementos[memento.GetType()](memento);
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
				this.RegisterType(type);
		}

		private void RegisterType(Type type)
		{
			var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
			foreach (var method in methods)
				this.RegisterType(method);
		}
		private void RegisterType(MethodInfo method)
		{
			if (method.IsGenericMethod)
				return;

			switch (method.Name)
			{
				case FactoryMethodName:
					this.RegisterFactory(method);
					break;
				case KeyMethodName:
					this.RegisterKey(method);
					break;
			}
		}
		private void RegisterFactory(MethodInfo method)
		{
			if (!typeof(IHydratable).IsAssignableFrom(method.ReturnType))
				return;

			var parameters = method.GetParameters();
			if (parameters.Length == 1)
				this.RegisterMementoFactory(method, parameters);
			if (parameters.Length == 2)
				this.RegisterMessageFactory(method, parameters);
		}
		private void RegisterMementoFactory(MethodInfo method, IList<ParameterInfo> parameters)
		{
			var mementoType = parameters[0].ParameterType;
			if (mementoType == typeof(object))
				return;

			var callback = Delegate.CreateDelegate(typeof(MementoDelegate<>).MakeGenericType(mementoType), method);
			RegisterMementoMethod.MakeGenericMethod(mementoType).Invoke(this, new object[] { callback });
		}
		private void RegisterMessageFactory(MethodInfo method, IList<ParameterInfo> parameters)
		{
		}
		private void RegisterKey(MethodInfo method)
		{
		}

// ReSharper disable UnusedMember.Local
		private void RegisterMemento<T>(MementoDelegate<T> callback)
		{
			this.mementos[typeof(T)] = x => callback((T)x);
		}
// ReSharper restore UnusedMember.Local

		private const string FactoryMethodName = "Create";
		private const string KeyMethodName = "Key";
		private static readonly MethodInfo RegisterMementoMethod = typeof(ConventionRoutingTable).GetMethod("RegisterMemento", BindingFlags.Instance | BindingFlags.NonPublic);
		private readonly Dictionary<Type, MementoDelegate> mementos = new Dictionary<Type, MementoDelegate>();
		private readonly Dictionary<Type, MessageDelegate> messages = new Dictionary<Type, MessageDelegate>();
	}

	internal delegate string KeyDelegate(object message, Dictionary<string, string> headers);
	internal delegate IHydratable MessageDelegate(object message, Dictionary<string, string> headers);
	internal delegate IHydratable MementoDelegate(object memento);
	internal delegate IHydratable MementoDelegate<T>(T memento);
}