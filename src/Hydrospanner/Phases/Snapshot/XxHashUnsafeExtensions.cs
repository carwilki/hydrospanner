namespace Hydrospanner.Phases.Snapshot
{
	internal static class XxHashUnsafeExtensions
	{
		// Reference: https://github.com/EventStore/EventStore/blob/master/src/EventStore/EventStore.Core/Index/Hashes/XXHashUnsafe.cs

		public static unsafe uint ComputeHash(this byte[] data, int offset = 0, uint len = 0, uint seed = Seed)
		{
			fixed (byte* input = &data[offset])
			{
				if (len < 16)
					return HashSmall(input, len, seed);

				return HashBig(input, len, seed);
			}
		}

		private static unsafe uint HashSmall(byte* data, uint len, uint seed)
		{
			var p = data;
			var bEnd = data + len;
			var limit = bEnd - 4;

			var idx = seed + Prime1;
			uint crc = Prime5;

			while (p < limit)
			{
				crc += (*(uint*)p) + idx;
				idx++;
				crc += Rotl32(crc, 17) * Prime4;
				crc *= Prime1;
				p += 4;
			}

			while (p < bEnd)
			{
				crc += (*p) + idx;
				idx++;
				crc *= Prime1;
				p++;
			}

			crc += len;

			crc ^= crc >> 15;
			crc *= Prime2;
			crc ^= crc >> 13;
			crc *= Prime3;
			crc ^= crc >> 16;

			return crc;
		}

		static unsafe uint HashBig(byte* data, uint len, uint seed)
		{
			var v1 = seed + Prime1;
			var v2 = (v1 * Prime2) + len;
			var v3 = v2 * Prime3;
			var v4 = v3 * Prime4;

			var p = (uint*)data;
			var limit = (uint*)(data + len - 16);

			while (p < limit)
			{
				v1 += Rotl32(v1, 13);
				v1 *= Prime1;
				v1 += *p;
				p++;
				v2 += Rotl32(v2, 11);
				v2 *= Prime1;
				v2 += *p;
				p++;
				v3 += Rotl32(v3, 17);
				v3 *= Prime1;
				v3 += *p;
				p++;
				v4 += Rotl32(v4, 19);
				v4 *= Prime1;
				v4 += *p;
				p++;
			}

			p = limit;
			v1 += Rotl32(v1, 17);
			v2 += Rotl32(v2, 19);
			v3 += Rotl32(v3, 13);
			v4 += Rotl32(v4, 11);
			v1 *= Prime1;
			v2 *= Prime1;
			v3 *= Prime1;
			v4 *= Prime1;
			v1 += *p;
			p++;
			v2 += *p;
			p++;
			v3 += *p;
			p++;
			v4 += *p;
			v1 *= Prime2;
			v2 *= Prime2;
			v3 *= Prime2;
			v4 *= Prime2;
			v1 += Rotl32(v1, 11);
			v2 += Rotl32(v2, 17);
			v3 += Rotl32(v3, 19);
			v4 += Rotl32(v4, 13);
			v1 *= Prime3;
			v2 *= Prime3;
			v3 *= Prime3;
			v4 *= Prime3;

			var crc = v1 + Rotl32(v2, 3) + Rotl32(v3, 6) + Rotl32(v4, 9);
			crc ^= crc >> 11;
			crc += (Prime4 + len) * Prime1;
			crc ^= crc >> 15;
			crc *= Prime2;
			crc ^= crc >> 13;
			return crc;
		}

		private static uint Rotl32(uint x, int r)
		{
			return (x << r) | (x >> (32 - r));
		}

		private const uint Seed = 0xc58f1a7b;
		private const uint Prime1 = 2654435761U;
		private const uint Prime2 = 2246822519U;
		private const uint Prime3 = 3266489917U;
		private const uint Prime4 = 668265263U;
		private const int Prime5 = 0x165667b1;
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