namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;

	public class PhotographicMemoryStream : MemoryStream
	{
		public byte[] Contents
		{
			get
			{
				if (this.buffer == null)
					return this.ToArray();

				var copy = new byte[this.buffer.Length];
				this.buffer.CopyTo(copy, 0);
				return copy;
			}
		}
		public bool Disposed { get; private set; }

		protected override void Dispose(bool disposing)
		{
			if (this.buffer == null)
				this.buffer = this.ToArray();
			
			this.Disposed = true;

			base.Dispose(disposing);
		}
		
		private byte[] buffer;
	}
}