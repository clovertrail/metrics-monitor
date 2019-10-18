//-------------------------------------------------------------------------------------------------
// <copyright file="BitBinaryReaderV2.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.BitHelper
{
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The class which allows reading bits from stream one by one.
    /// </summary>
    public sealed class BitBinaryReaderV2
    {
        private readonly BinaryReader reader;
        private int currentBit;
        private byte currentByte;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitBinaryReaderV2"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public BitBinaryReaderV2(BinaryReader reader)
        {
            this.reader = reader;
            this.currentBit = 128;
        }

        /// <summary>
        /// Gett the index of the current bit from which data is read.
        /// </summary>
        public byte CurrentBitIndex { get; private set; }

        /// <summary>
        /// Gets the <see cref="BinaryReader"/>.
        /// </summary>
        public BinaryReader BinaryReader => this.reader;

        /// <summary>
        /// Reads bits from the stream.
        /// </summary>
        /// <param name="numBits">The number of bits.</param>
        /// <returns>
        /// Read bit.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadBits(int numBits)
        {
            long result = 0;
            int i;

            // First complete the current byte read
            for (i = numBits - 1; i >= 0 && this.CurrentBitIndex != 0; --i)
            {
                if (this.ReadBit())
                {
                    result |= 1L << i;
                }
            }

            // Now read byte by byte
            for (; i >= 7; i -= 8)
            {
                result |= ((long)this.GetCurrentByte()) << (i - 7);
            }

            // Now read the left bits
            for (; i >= 0; --i)
            {
                if (this.ReadBit())
                {
                    result |= 1L << i;
                }
            }

            return result;
        }

        /// <summary>
        /// Reads one bit from the stream.
        /// </summary>
        /// <returns>Read bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBit()
        {
            var result = (this.GetCurrentByte() & this.currentBit) != 0;
            if (this.currentBit == 1)
            {
                this.currentBit = 128;
                this.CurrentBitIndex = 0;
            }
            else
            {
                this.currentBit >>= 1;
                ++this.CurrentBitIndex;
            }

            return result;
        }

        /// <summary>
        /// Reads uint value stored in Base-128 encoding.
        /// </summary>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32FromBase128()
        {
            return (uint)this.ReadUInt64FromBase128();
        }

        /// <summary>
        /// Reads ulong value stored in Base-128 encoding.
        /// </summary>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64FromBase128()
        {
            ulong val = 0;
            var shift = 0;
            byte b;
            do
            {
                b = (byte)this.ReadBits(8);
                val = val + ((ulong)(b & 0x7f) << shift);
                shift += 7;
            }
            while ((b & 0x80) != 0);
            return val;
        }

        /// <summary>
        /// Reads till end of byte boundary is reached.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadTillEndOfByteBoundary()
        {
            while (this.currentBit != 128)
            {
                this.ReadBit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetCurrentByte()
        {
            if (this.currentBit == 128)
            {
                this.currentByte = this.reader.ReadByte();
            }

            return this.currentByte;
        }
    }
}
