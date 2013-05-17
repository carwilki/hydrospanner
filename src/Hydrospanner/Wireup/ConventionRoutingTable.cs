namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using log4net;

	public sealed class ConventionRoutingTable : IRoutingTable
	{
		public IEnumerable<HydrationInfo> Lookup<T>(Delivery<T> delivery)
		{
			this.routes.Clear();

			List<LookupDelegate> delegates;
			if (!this.lookups.TryGetValue(typeof(Delivery<T>), out delegates))
				return this.routes;

			foreach (var item in delegates)
				this.routes.Add(item(delivery));

			return this.routes;
		}
		public IHydratable Restore<T>(string key, T memento)
		{
			MementoDelegate callback;
			return this.mementos.TryGetValue(typeof(T), out callback) ? callback(memento) : null;
		}

		public ConventionRoutingTable()
		{
		}
		public ConventionRoutingTable(IEnumerable<Assembly> assemblies) : this(assemblies.SelectMany(x => x.GetTypes()))
		{
		}
		public ConventionRoutingTable(params Assembly[] assemblies) : this((IEnumerable<Assembly>)assemblies)
		{
		}
		public ConventionRoutingTable(params Type[] types) : this((IEnumerable<Type>)types)
		{
		}
		public ConventionRoutingTable(IEnumerable<Type> types)
		{
			var currentAssemblyTypes = this.GetType().Assembly.GetTypes();
			for (var i = 0; i < currentAssemblyTypes.Length; i++)
				this.RegisterType(currentAssemblyTypes[i]);

			foreach (var type in types ?? new Type[0])
				this.RegisterType(type);
		}

		private void RegisterType(Type type)
		{
			Log.DebugFormat("Attempting to register type ({0}) with the ConventionRoutingTable.", type);

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
					this.RegisterMemento(method);
					break;
				case LookupMethodName:
					this.RegisterLookup(method);
					break;
			}
		}
		private void RegisterMemento(MethodInfo method)
		{
			if (!typeof(IHydratable).IsAssignableFrom(method.ReturnType))
				return;

			var parameters = method.GetParameters();
			if (parameters.Length != 1)
				return;

			var mementoType = parameters[0].ParameterType;
			if (mementoType == typeof(object))
				return;

			// TODO: we can now register the same memento multiple times
			if (this.mementos.ContainsKey(mementoType))
				throw new InvalidOperationException("Memento of type '{0}' cannot be registered multiple times.".FormatWith(mementoType));

			Log.DebugFormat(
				"Registering memento restoration method '{0}.{1}({2} memento)' with the ConventionRoutingTable.", 
				(method.DeclaringType ?? typeof(IHydratable)).Name, 
				method.Name, 
				mementoType.Name);

			var callback = Delegate.CreateDelegate(typeof(MementoDelegate<>).MakeGenericType(mementoType), method);
			RegisterMementoMethod.MakeGenericMethod(mementoType).Invoke(this, new object[] { callback });
		}
		private void RegisterLookup(MethodInfo method)
		{
			if (!typeof(HydrationInfo).IsAssignableFrom(method.ReturnType))
				return;

			var parameters = method.GetParameters();
			if (parameters.Length != 1)
				return;

			var messageType = parameters[0].ParameterType;
			Log.DebugFormat(
				"Registering lookup method ({0}.{1}({2} message, ...) with the ConventionRoutingTable.", 
				(method.DeclaringType ?? typeof(IHydratable)).Name,
				method.Name,
				messageType.Name);

			var generic = typeof(LookupDelegate<>).MakeGenericType(messageType);
			var callback = Delegate.CreateDelegate(generic, method);
			RegisterLookupMethod.MakeGenericMethod(messageType).Invoke(this, new object[] { callback });
		}

// ReSharper disable UnusedMember.Local
		private void RegisterGenericMemento<T>(MementoDelegate<T> callback)
		{
			this.mementos[typeof(T)] = x => callback((T)x);
		}
		private void RegisterGenericLookup<T>(LookupDelegate<T> callback)
		{
			List<LookupDelegate> delegates;
			if (!this.lookups.TryGetValue(typeof(T), out delegates))
				this.lookups[typeof(T)] = delegates = new List<LookupDelegate>();

			delegates.Add(x => callback((T)x));
		}
// ReSharper restore UnusedMember.Local

		private const string FactoryMethodName = "Restore";
		private const string LookupMethodName = "Lookup";
		private static readonly MethodInfo RegisterMementoMethod = typeof(ConventionRoutingTable).GetMethod("RegisterGenericMemento", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly MethodInfo RegisterLookupMethod = typeof(ConventionRoutingTable).GetMethod("RegisterGenericLookup", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly ILog Log = LogManager.GetLogger(typeof(ConventionRoutingTable));
		private readonly Dictionary<Type, MementoDelegate> mementos = new Dictionary<Type, MementoDelegate>(128);
		private readonly Dictionary<Type, List<LookupDelegate>> lookups = new Dictionary<Type, List<LookupDelegate>>(128);
		private readonly List<HydrationInfo> routes = new List<HydrationInfo>(16);
	}

	internal delegate HydrationInfo LookupDelegate(object delivery);
	internal delegate HydrationInfo LookupDelegate<T>(T delivery);
	internal delegate IHydratable MementoDelegate(object memento);
	internal delegate IHydratable MementoDelegate<T>(T memento);
}