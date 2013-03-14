namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
	using System.Security.Cryptography;
	using System.Text;

	public class SnapshotStreamReader : IDisposable
	{
		public int Count { get; private set; }
		public long MessageSequence { get; private set; }
		public int Iteration { get; private set; }

		public IEnumerable<KeyValuePair<Type, byte[]>> Read()
		{
			if (this.Count == 0)
				yield break;

			while (this.stream.Position < this.stream.Length)
			{
				var type = this.ResolveType(this.Next());
				var item = this.Next();
				yield return new KeyValuePair<Type, byte[]>(type, item);
			}
		}

		private Type ResolveType(byte[] rawType)
		{
			var typeName = Encoding.UTF8.GetString(rawType);
			return this.types.ValueOrDefault(typeName) ?? (this.types[typeName] = Type.GetType(typeName));
		}

		private byte[] Next()
		{
			this.stream.Read(this.lengthBuffer, 0, this.lengthBuffer.Length);
			var length = BitConverter.ToInt32(this.lengthBuffer, 0);
			var thingBuffer = new byte[length];
			this.stream.Read(thingBuffer, 0, length);
			return thingBuffer;
		}

		public static SnapshotStreamReader Open(long sequence, int snapshotIteration, string hash, Stream stream)
		{
			hash = hash.Trim().ToUpperInvariant();
			var computed = ComputeHash(stream).ToUpperInvariant();
			stream.Position = 0;

			if (hash == computed)
				return new SnapshotStreamReader(sequence, snapshotIteration, stream);

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

		private SnapshotStreamReader(long sequence, int snapshotIteration, Stream stream)
		{
			this.MessageSequence = sequence;
			this.Iteration = snapshotIteration;
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
		readonly IDictionary<string, Type> types = new Dictionary<string, Type>();
		readonly byte[] lengthBuffer = new byte[sizeof(int)];
	}
}