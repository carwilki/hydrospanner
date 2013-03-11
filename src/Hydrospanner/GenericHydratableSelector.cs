namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;

	public class GenericHydratableSelector : IHydratableSelector
	{
		public void Register<T>(IHydratableSelector<T> selector)
		{
			this.selectors.Add(typeof(T), new GenericAdapter<T>(selector));
		}
		public IEnumerable<IHydratableKey> Keys(object message, Dictionary<string, string> headers = null)
		{
			IHydratableSelector selector;
			if (this.selectors.TryGetValue(message.GetType(), out selector))
				return selector.Keys(message, headers);

			return new IHydratableKey[0];
		}
		public IHydratable Create(object memento)
		{
			throw new NotImplementedException(); // TODO
		}

		private readonly Dictionary<Type, IHydratableSelector> selectors = new Dictionary<Type, IHydratableSelector>();

		private class GenericAdapter<T> : IHydratableSelector
		{
			public IEnumerable<IHydratableKey> Keys(object message, Dictionary<string, string> headers = null)
			{
				return this.selector.Keys((T)message, headers);
			}
			public IHydratable Create(object memento)
			{
				throw new NotSupportedException();
			}
			public GenericAdapter(IHydratableSelector<T> selector)
			{
				this.selector = selector;
			}
			private readonly IHydratableSelector<T> selector;
		}
	}
}