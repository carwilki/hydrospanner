namespace Hydrospanner.Phases.Snapshot
{
	using System.IO;

	public class PhotographicMemoryStream : MemoryStream
	{
		public byte[] Array { get { return this.buffer ?? this.ToArray(); } }

		protected override void Dispose(bool disposing)
		{
			if (this.buffer == null)
				this.buffer = this.ToArray();

			base.Dispose(disposing);
		}
		
		private byte[] buffer;
	}
}