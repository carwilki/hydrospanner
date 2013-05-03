namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;

	public sealed class CommandFilterTransformer : ITransformer
	{
		public IEnumerable<object> Transform<T>(Delivery<T> delivery)
		{
			if (delivery.Live || !delivery.Message.GetType().Name.EndsWith("Command"))
				return this.inner.Transform(delivery);

			return Empty;
		}

		public CommandFilterTransformer(ITransformer inner)
		{
			if (inner == null)
				throw new ArgumentNullException("inner");

			this.inner = inner;
		}

		private static readonly object[] Empty = new object[0];
		private readonly ITransformer inner;
	}
}