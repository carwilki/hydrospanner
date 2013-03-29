﻿namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public class ConventionRoutingTable : IRoutingTable
	{
		public IEnumerable<HydrationInfo> Lookup(object message, Dictionary<string, string> headers)
		{
			if (message == null)
				return null;

			this.routes.Clear();

			List<LookupDelegate> delegates;
			if (!this.lookups.TryGetValue(message.GetType(), out delegates))
				return this.routes;

			foreach (var item in delegates)
				this.routes.Add(item(message, headers));

			return this.routes;
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

			if (this.mementos.ContainsKey(mementoType))
				throw new InvalidOperationException("Memento of type '{0}' cannot be registered multiple times.".FormatWith(mementoType));

			var callback = Delegate.CreateDelegate(typeof(MementoDelegate<>).MakeGenericType(mementoType), method);
			RegisterMementoMethod.MakeGenericMethod(mementoType).Invoke(this, new object[] { callback });
		}
		private void RegisterLookup(MethodInfo method)
		{
			if (!typeof(HydrationInfo).IsAssignableFrom(method.ReturnType))
				return;

			var parameters = method.GetParameters();
			if (parameters.Length != 2)
				return;

			var messageType = parameters[0].ParameterType;
			if (messageType == typeof(object))
				return;

			if (parameters[1].ParameterType != typeof(Dictionary<string, string>))
				return;

			var callback = Delegate.CreateDelegate(typeof(LookupDelegate<>).MakeGenericType(messageType), method);
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

			delegates.Add((message, headers) => callback((T)message, headers));
		}
// ReSharper restore UnusedMember.Local

		private const string FactoryMethodName = "Create";
		private const string LookupMethodName = "Lookup";
		private static readonly MethodInfo RegisterMementoMethod = typeof(ConventionRoutingTable).GetMethod("RegisterGenericMemento", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly MethodInfo RegisterLookupMethod = typeof(ConventionRoutingTable).GetMethod("RegisterGenericLookup", BindingFlags.Instance | BindingFlags.NonPublic);
		private readonly Dictionary<Type, MementoDelegate> mementos = new Dictionary<Type, MementoDelegate>(128);
		private readonly Dictionary<Type, List<LookupDelegate>> lookups = new Dictionary<Type, List<LookupDelegate>>(128);
		private readonly List<HydrationInfo> routes = new List<HydrationInfo>(16);
	}

	internal delegate HydrationInfo LookupDelegate(object message, Dictionary<string, string> headers);
	internal delegate HydrationInfo LookupDelegate<T>(T message, Dictionary<string, string> headers);
	internal delegate IHydratable MementoDelegate(object memento);
	internal delegate IHydratable MementoDelegate<T>(T memento);
}