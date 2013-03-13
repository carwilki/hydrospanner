namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;

	public class SnapshotStreamReader : IDisposable
	{
		public int Count { get; private set; }

		public long MessageSequence { get; private set; }

		public IEnumerable<byte[]> Read()
		{
			if (this.Count == 0)
				yield break;

			var lengthBuffer = new byte[sizeof(int)];
			while (this.stream.Position < this.stream.Length)
			{
				this.stream.Read(lengthBuffer, 0, lengthBuffer.Length);
				var length = BitConverter.ToInt32(lengthBuffer, 0);

				var itemBuffer = new byte[length];
				this.stream.Read(itemBuffer, 0, itemBuffer.Length);
				yield return itemBuffer;
			}
		}

		public static SnapshotStreamReader Open(long sequence, string hash, Stream stream)
		{
			hash = hash.Trim().ToUpperInvariant();
			var computed = ComputeHash(stream).ToUpperInvariant();
			stream.Position = 0;

			if (hash == computed)
				return new SnapshotStreamReader(sequence, stream);

			return null;
		}
		private static string ComputeHash(Stream stream)
		{
			using (var hasher = new SHA1Managed())
			{
				var computed = hasher.ComputeHash(stream);
				return new SoapHexBinary(computed).ToString();
			}
		}

		private SnapshotStreamReader(long sequence, Stream stream)
		{
			this.MessageSequence = sequence;
			this.stream = stream;

			var countBuffer = new byte[sizeof(int)];
			stream.Read(countBuffer, 0, sizeof(int));
			this.Count = BitConverter.ToInt32(countBuffer, 0);
		}
		public SnapshotStreamReader()
		{
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.stream.Dispose();
		}

		readonly Stream stream;
	}
}