namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;

	public class SystemSnapshotRecorder
	{
		public SnapshotInputStream Read()
		{
			var fullPaths = Directory.EnumerateFiles(this.path, this.prefix + "-*", SearchOption.TopDirectoryOnly).OrderByDescending(x => x).ToArray();

			foreach (var fullPath in fullPaths)
			{
				var filename = Path.GetFileNameWithoutExtension(fullPath) ?? string.Empty;
				var values = filename.Split("-".ToCharArray());
				if (values.Length != 3)
					continue;

				var sequence = long.Parse(values[1]);
				var hash = values[2];
				var fileStream = File.OpenRead(fullPath);

				var stream = SnapshotInputStream.Open(sequence, hash, fileStream);
				if (stream != null)
					return stream;
			}

			return new SnapshotInputStream();
		}

		public SnapshotOutputStream Create(long messageSequence, int itemCount)
		{
			var name = Format.FormatWith(this.prefix, messageSequence);
			var fullname = Path.Combine(this.path, name);

			var stream = new FileStream(fullname, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, BufferSize, FileOptions.RandomAccess);
			return new SnapshotOutputStream(stream, itemCount, checksum => File.Move(fullname, Format.FormatWith(fullname, checksum)));
		}

		public SystemSnapshotRecorder(string path, string prefix)
		{
			this.path = path;
			this.prefix = prefix;
		}

		private const string Format = "{0}-{1}";
		private const int BufferSize = 1024 * 512;
		private readonly string prefix;
		private readonly string path;
	}

	public class SnapshotInputStream : IDisposable
	{
		public long Sequence
		{
			get { return this.sequence; }
		}
		public int ItemCount { get; private set; }

		public IEnumerable<byte[]> Items
		{
			get
			{
				if (this.ItemCount == 0)
					yield break;

				var lengthBuffer = new byte[4];
				while (this.stream.Position < this.stream.Length)
				{
					this.stream.Read(lengthBuffer, 0, lengthBuffer.Length);
					var length = BitConverter.ToInt32(lengthBuffer, 0);

					var itemBuffer = new byte[length];
					this.stream.Read(itemBuffer, 0, itemBuffer.Length);
					yield return itemBuffer;
				}
			}
		}

		public static SnapshotInputStream Open(long sequence, string hash, Stream stream)
		{
			hash = hash.Trim().ToUpperInvariant();
			var computed = ComputeHash(stream).ToUpperInvariant();
			stream.Position = 0;

			if (hash == computed)
				return new SnapshotInputStream(sequence, stream);

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

		public SnapshotInputStream()
		{
		}
		private SnapshotInputStream(long sequence, Stream stream)
		{
			this.sequence = sequence;
			this.stream = stream;

			var countBuffer = new byte[4];
			stream.Read(countBuffer, 0, 4);
			this.ItemCount = BitConverter.ToInt32(countBuffer, 0);
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

		private readonly long sequence;
		private readonly Stream stream;
	}

	public class SnapshotOutputStream : IDisposable
	{
		public void WriteItem(byte[] serialized)
		{
			if (this.complete == 0)
				this.stream.Write(BitConverter.GetBytes(this.count), 0, 4); // total number of items in the snapshot

			this.stream.Write(BitConverter.GetBytes(serialized.Length), 0, 4); // length of each item (not including this header)
			this.stream.Write(serialized, 0, serialized.Length); // the item itself

			if (++this.complete < this.count)
				return; // more to do

			this.stream.Position = 0;
			var hash = ComputeHash(this.stream);

			this.stream.Flush();
			this.stream.Dispose();
			
			this.finalize(hash);
		}
		private static string ComputeHash(Stream stream)
		{
			using (var hasher = new SHA1Managed())
			{
				var computed = hasher.ComputeHash(stream);
				return new SoapHexBinary(computed).ToString();
			}
		}

		public SnapshotOutputStream(Stream stream, int count, Action<string> finalize)
		{
			this.stream = stream;
			this.count = count;
			this.finalize = finalize;
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

		private readonly Stream stream;
		private readonly int count;
		private readonly Action<string> finalize;
		private int complete;
	}
}