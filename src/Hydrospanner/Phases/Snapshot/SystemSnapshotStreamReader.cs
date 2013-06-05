namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;
	using log4net;

	public class SystemSnapshotStreamReader : IDisposable
	{
		public virtual int Count { get; private set; }
		public virtual long MessageSequence { get; private set; }
		public virtual IEnumerable<Tuple<string, string, byte[]>> Read()
		{
			if (this.Count == 0)
				yield break;

			var counter = 0;

			while (this.stream.Position < this.stream.Length)
			{
				var keyBytes = this.Next();
				var key = ResolveString(keyBytes);

				var typeBytes = this.Next();
				var type = ResolveString(typeBytes);

				var itemBytes = this.Next();
				if (itemBytes != null && itemBytes.Length == 0)
					itemBytes = null;

				if (counter % 10000 == 0)
					Log.InfoFormat("Read {0} mementos from the snapshot", counter);

				counter++;
				yield return new Tuple<string, string, byte[]>(key, type, itemBytes);
			}
		}

		private static string ResolveString(byte[] rawType)
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
		public static SystemSnapshotStreamReader Open(long sequence, string hash, Stream stream)
		{
			hash = hash.Trim().ToUpperInvariant();
			var computed = ComputeHash(stream).ToUpperInvariant();
			stream.Position = 0;
			if (hash == computed)
				return new SystemSnapshotStreamReader(sequence, stream);

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

		private SystemSnapshotStreamReader(long sequence, Stream stream)
		{
			this.MessageSequence = sequence;
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
				this.stream.TryDispose();
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(SystemSnapshotStreamReader));
		private readonly Stream stream;
		private readonly byte[] lengthBuffer = new byte[sizeof(int)];
	}
}