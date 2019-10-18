//-------------------------------------------------------------------------------------------------
// <copyright file="BitBinaryWriterV2.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.BitHelper
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// A class which allows writing bits to the stream.
    /// We accumulate current byte value bit by bit and when it is full, it is written to the stream.
    /// <see cref="Flush" /> method must be called at the end since the last byte to write to stream could be partial.
    /// </summary>
    public sealed class BitBinaryWriterV2
    {
        private readonly BinaryWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitBinaryWriterV2"/> class.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public BitBinaryWriterV2(BinaryWriter writer)
        {
            this.writer = writer;
            this.CurrentBitIndex = 0;
            this.CurrentByte = 0;
        }

        /// <summary>
        /// Gets the current bit position in the buffer.
        /// </summary>
        public byte CurrentBitIndex { get; private set; }

        /// <summary>
        /// Gets the value of the currently accumulated byte.
        /// </summary>
        public byte CurrentByte { get; private set; }

        /// <summary>
        /// Gets the underlying binary writer.
        /// </summary>
        public BinaryWriter BinaryWriter => this.writer;

        /// <summary>
        /// Write bits to the stream.
        /// </summary>
        /// <param name="oldValue">The value.</param>
        /// <param name="numBits">The number bits.</param>
        /// <param name="positionOfLeastSignificantBit">The position of least significant bit, starting with 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBits(long oldValue, int numBits, int positionOfLeastSignificantBit)
        {
            var l = &oldValue;
            var value = *(ulong*)l;
            int endBit = 64 - positionOfLeastSignificantBit;
            int startBit = 64 - (numBits + positionOfLeastSignificantBit);
            var bitsLeftToCopy = endBit - startBit;
            value <<= startBit;

            while (bitsLeftToCopy > 0)
            {
                var bitsToCopy = Math.Min(bitsLeftToCopy, 8 - this.CurrentBitIndex);
                this.CurrentByte = (byte)(((ulong)this.CurrentByte << (bitsToCopy - 1)) | (value >> (64 - bitsToCopy)));

                value <<= bitsToCopy;
                bitsLeftToCopy -= bitsToCopy;

                this.CurrentBitIndex += (byte)bitsToCopy;
                if (this.CurrentBitIndex == 8)
                {
                    this.writer.Write(this.CurrentByte);
                    this.CurrentByte = 0;
                    this.CurrentBitIndex = 0;
                }
                else
                {
                    this.CurrentByte <<= 1;
                }
            }
        }

        /// <summary>
        /// Write bit to the stream.
        /// </summary>
        /// <param name="bit">Bit value to write, where True means 1 and False means 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBit(bool bit)
        {
            this.CurrentByte = (byte)(this.CurrentByte | (bit ? 1 : 0));
            if (++this.CurrentBitIndex == 8)
            {
                this.writer.Write(this.CurrentByte);
                this.CurrentByte = 0;
                this.CurrentBitIndex = 0;
            }
            else
            {
                this.CurrentByte <<= 1;
            }
        }

        /// <summary>
        /// Writes uint value Base-128 encoded.
        /// </summary>
        /// <param name="value">Value to write.</param>
        public void WriteUInt32AsBase128(uint value)
        {
            this.WriteUInt64AsBase128(value);
        }

        /// <summary>
        /// Writes ulong value Base-128 encoded.
        /// </summary>
        /// <param name="value">Value to write.</param>
        public void WriteUInt64AsBase128(ulong value)
        {
            var t = value;
            do
            {
                var b = (byte)(t & 0x7f);
                t >>= 7;
                if (t > 0)
                {
                    b |= 0x80;
                }

                this.WriteBits(b, 8, 0);
            }
            while (t > 0);
        }

        /// <summary>
        /// Flush the current byte into stream even if it is partial.
        /// </summary>
        public void WriteTillEndOfByteBoundary()
        {
            if (this.CurrentBitIndex != 0)
            {
                var value = this.CurrentByte << (7 - this.CurrentBitIndex);
                this.writer.Write((byte)value);
                this.CurrentBitIndex = 0;
                this.CurrentByte = 0;
                this.CurrentBitIndex = 0;
            }
        }

        /// <summary>
        /// Flush the current byte into stream even if it is partial.
        /// </summary>
        public void Flush()
        {
            if (this.CurrentBitIndex != 0)
            {
                var value = this.CurrentByte << (7 - this.CurrentBitIndex);
                this.writer.Write((byte)value);
            }
        }
    }
}
