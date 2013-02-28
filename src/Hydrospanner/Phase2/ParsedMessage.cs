namespace Hydrospanner.Phase2
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using Hydrospanner;

	public sealed class ParsedMessage
	{
		public Guid StreamId { get; set; }
		public List<IHydratable> Hydratables { get; set; }
		public object Body { get; set; }
		public Hashtable Headers { get; set; }
		public Action ConfirmDelivery { get; set; }

		public void Clear()
		{
			this.StreamId = Guid.Empty;
			this.Hydratables = null;
			this.Headers = null;
			this.Body = null;
			this.ConfirmDelivery = null;
		}
	}
}