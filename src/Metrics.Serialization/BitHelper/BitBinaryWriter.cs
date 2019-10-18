//-------------------------------------------------------------------------------------------------
// <copyright file="BitBinaryWriter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.BitHelper
{
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// A class which allows writing bits to the stream.
    /// We accumulate current byte value bit by bit and when it is full, it is written to the stream.
    /// <see cref="Flush" /> method must be called at the end since the last byte to write to stream could be partial.
    /// </summary>
    public sealed class BitBinaryWriter
    {
        private const int HighestBitInByte = 1 << 7;
        private readonly BinaryWriter writer;
        private int currentBit;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitBinaryWriter"/> class.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public BitBinaryWriter(BinaryWriter writer)
        {
            this.writer = writer;

            this.currentBit = HighestBitInByte;
            this.CurrentByte = 0;
        }

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
        /// <param name="value">The value.</param>
        /// <param name="numBits">The number bits.</param>
        /// <param name="positionOfLeastSignificantBit">The position of least significant bit, starting with 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBits(long value, int numBits, int positionOfLeastSignificantBit)
        {
            for (int i = numBits; i > 0; --i)
            {
                long mask = 1L << (i - 1 + positionOfLeastSignificantBit);

                bool bit = (value & mask) != 0;
                this.CurrentByte = (byte)(this.CurrentByte | (bit ? this.currentBit : 0));

                this.currentBit >>= 1;

                if (this.currentBit == 0)
                {
                    this.writer.Write(this.CurrentByte);
                    this.CurrentByte = 0;
                    this.currentBit = HighestBitInByte;
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
            this.CurrentByte = (byte)(this.CurrentByte | (bit ? this.currentBit : 0));

            this.currentBit >>= 1;

            if (this.currentBit == 0)
            {
                this.writer.Write(this.CurrentByte);
                this.CurrentByte = 0;
                this.currentBit = HighestBitInByte;
            }
        }

        /// <summary>
        /// Flush the current byte into stream even if it is partial.
        /// </summary>
        public void Flush()
        {
            if (this.currentBit != HighestBitInByte)
            {
                this.writer.Write(this.CurrentByte);
                this.CurrentByte = 0;
                this.currentBit = HighestBitInByte;
            }
        }
    }
}
