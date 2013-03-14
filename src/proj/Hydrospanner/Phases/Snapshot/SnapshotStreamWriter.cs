namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.IO;

	internal class SnapshotStreamWriter : IDisposable
	{
		public void Write(byte[] serialized)
		{
			// TODO
		}

		private SnapshotStreamWriter(Stream stream, int items)
		{
			this.stream = stream;
			this.items = items;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// TODO
		}

		readonly Stream stream;
		readonly int items;
	}
}