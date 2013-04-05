namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;

	public class SystemSnapshotStreamReader : IDisposable
	{
		public virtual int Count { get; private set; }
		public virtual long MessageSequence { get; private set; }
		public virtual int Generation { get; private set; }
		public virtual IEnumerable<KeyValuePair<string, byte[]>> Read()
		{
			if (this.Count == 0)
				yield break;

			while (this.stream.Position < this.stream.Length)
			{
				var type = ResolveType(this.Next());
				var item = this.Next();
				yield return new KeyValuePair<string, byte[]>(type, item);
			}
		}

		private static string ResolveType(byte[] rawType)
		{
			return rawType.SliceString(0);
		}
		private byte[] Next()
		{
			this.stream.Read(this.lengthBuffer, 0, this.lengthBuffer.Length);
			var length = this.lengthBuffer.SliceInt32(0);
			var itemBuffer = new byte[length];
			this.stream.Read(itemBuffer, 0, length);
			return itemBuffer;
		}
		public static SystemSnapshotStreamReader Open(long sequence, int snapshotGeneration, string hash, Stream stream)
		{
			hash = hash.Trim().ToUpperInvariant();
			var computed = ComputeHash(stream).ToUpperInvariant();
			stream.Position = 0;
			if (hash == computed)
				return new SystemSnapshotStreamReader(sequence, snapshotGeneration, stream);

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

		private SystemSnapshotStreamReader(long sequence, int snapshotGeneration, Stream stream)
		{
			this.MessageSequence = sequence;
			this.Generation = snapshotGeneration;
			this.stream = stream;

			var countBuffer = new byte[sizeof(int)];
			stream.Read(countBuffer, 0, sizeof(int));
			this.Count = countBuffer.SliceInt32(0);
		}
		public SystemSnapshotStreamReader()
		{
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && this.stream != null)
				this.stream.Dispose();
		}

		readonly Stream stream;
		readonly byte[] lengthBuffer = new byte[sizeof(int)];
	}
}