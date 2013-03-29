namespace Hydrospanner.Wireup
{
	using System;

	public struct HydrationInfo
	{
		public string Key { get; private set; }
		public IHydratable Create()
		{
			if (this.factory != null)
				return this.factory();

			return null;
		}

		public HydrationInfo(string key, Func<IHydratable> factory) : this()
		{
			if (key == null)
				throw new ArgumentNullException("key");

			if (factory == null)
				throw new ArgumentNullException("factory");

			this.Key = key;
			this.factory = factory;
		}

		private readonly Func<IHydratable> factory;
	}
}