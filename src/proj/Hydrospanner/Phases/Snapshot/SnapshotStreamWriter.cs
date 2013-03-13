namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.IO.Abstractions;

	public class SnapshotStreamWriter : IDisposable
	{
		public void Write(byte[] serialized)
		{
			// TODO
		}

		public static SnapshotStreamWriter Create(FileBase file, string location, int iteration, long messageSequence, int itemCount)
		{
			return new SnapshotStreamWriter();
		}

		private SnapshotStreamWriter()
		{
			// TODO
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
	}
}