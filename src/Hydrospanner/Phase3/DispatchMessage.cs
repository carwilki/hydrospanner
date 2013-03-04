namespace Hydrospanner.Phase3
{
	using System;

	public sealed class DispatchMessage
	{
		public object Body { get; set; }
		public byte[] Serialized { get; set; }
		public Action ConfirmDelivery { get; set; }
	}
}