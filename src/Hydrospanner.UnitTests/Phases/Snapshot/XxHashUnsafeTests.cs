#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

// Adapted Verification and Sanity checks from SMHasher: http://code.google.com/p/smhasher/
// Calculated from reference XXhash implementation: http://code.google.com/p/xxhash/
// Reference: https://github.com/EventStore/EventStore/blob/master/src/EventStore/EventStore.Core.Tests/Hashes/xxhash_hash_should.cs
// Reference: https://github.com/EventStore/EventStore/blob/master/src/EventStore/EventStore.Core.Tests/Hashes/SMHasher.cs

namespace Hydrospanner.Phases.Snapshot
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;

	[Subject(typeof(XxHashUnsafeExtensions))]
	public class xxhash_hash_should
	{
		It should_pass_the_verification_test = () =>
			VerificationTest().ShouldBeTrue();

		[Ignore("Long running...")]
		It should_pass_the_sanity_test = () =>
			SanityTest().ShouldBeTrue();

		const uint XxHashReferenceVerificationValue = 0x56D249B1;
	
		//-----------------------------------------------------------------------------
		// This should hopefully be a thorough and unambiguous test of whether a hash
		// is correctly implemented on a given platform
		public static bool VerificationTest(uint expected = XxHashReferenceVerificationValue)
		{
			const int HashBytes = 4;

			var key = new byte[256];
			var hashes = new byte[HashBytes * 256];

			// Hash keys of the form {0}, {0,1}, {0,1,2}... up to N=255,using 256-N as the seed
			for (int i = 0; i < 256; i++)
			{
				key[i] = (byte)i;

				var hash = key.ComputeHash(0, (uint)i, (uint)(256 - i));
				Buffer.BlockCopy(BitConverter.GetBytes(hash), 0, hashes, i * HashBytes, HashBytes);
			}

			// Then hash the result array

			var verification = hashes.ComputeHash(0, HashBytes * 256, 0);

			// The first four bytes of that hash, interpreted as a little-endian integer, is our verification value

			if (expected != verification)
			{
				Console.WriteLine("Verification value 0x{0:X8} : Failed! (Expected 0x{1:X8})", verification, expected);
				return false;
			}

			Console.WriteLine("Verification value 0x{0:X8} : Passed!", verification);
			return true;
		}

		//----------------------------------------------------------------------------
		// Basic sanity checks:
		//     - A hash function should not be reading outside the bounds of the key.
		//     - Flipping a bit of a key should, with overwhelmingly high probability, result in a different hash.
		//     - Hashing the same key twice should always produce the same result.
		//     - The memory alignment of the key should not affect the hash result.
		public static bool SanityTest()
		{
			var rnd = new Random(883741);

			var result = true;

			const int Reps = 10;
			const int KeyMax = 256;
			const int Pad = 16;
			const int BufferLength = KeyMax + (Pad * 3);

			var buffer1 = new byte[BufferLength];
			var buffer2 = new byte[BufferLength];

			for (var irep = 0; irep < Reps; irep++)
			{
				if (irep % (Reps / 10) == 0) Console.Write(".");

				for (var len = 4; len <= KeyMax; len++)
				{
					for (var offset = Pad; offset < Pad * 2; offset++)
					{
						rnd.NextBytes(buffer1);
						rnd.NextBytes(buffer2);

						Buffer.BlockCopy(buffer2, Pad + offset, buffer1, Pad, len);

						var hash1 = buffer1.ComputeHash(Pad, (uint)len, 0);

						for (var bit = 0; bit < (len * 8); bit++)
						{
							// Flip a bit, hash the key -> we should get a different result.
							Flipbit(buffer2, Pad + offset, len, bit);
							var hash2 = buffer2.ComputeHash(Pad + offset, (uint)len, 0);

							if (hash1 == hash2)
								result = false;

							// Flip it back, hash again -> we should get the original result.
							Flipbit(buffer2, Pad + offset, len, bit);
							hash2 = buffer2.ComputeHash(Pad + offset, (uint)len, 0);

							if (hash1 != hash2)
								result = false;
						}
					}
				}
			}
			return result;
		}

		private static void Flipbit(IList<byte> array, int offset, int len, int bit)
		{
			var byteNum = bit >> 3;
			bit = bit & 0x7;

			if (byteNum < len)
				array[offset + byteNum] ^= (byte)(1 << bit);
		}
	}
}

// Copyright (c) 2012, Event Store LLP
// All rights reserved.
//  
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//  
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414