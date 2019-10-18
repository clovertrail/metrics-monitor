//-------------------------------------------------------------------------------------------------
// <copyright file="DoubleValueSerializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Metrics.Serialization.BitHelper;

    /// <summary>
    /// This class provides set of methods to serialize double values using gorilla paper algorithm.
    /// </summary>
    public sealed class DoubleValueSerializer
    {
        private const int NumBitsToEncodeNumLeadingZeros = 5;
        private const int NumBitsToEncodeNumMeaningfulBits = 6;
        private const int MaxLeadingZerosLength = (1 << NumBitsToEncodeNumLeadingZeros) - 1;

        private static readonly double[] EmptyDoubleArray = new double[0];
        private static readonly double?[] EmptyNullableDoubleArray = new double?[0];

        /// <summary>
        /// Serializes the specified <paramref name="values"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="values">The values to serialize.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize(BinaryWriter writer, double[] values)
        {
            unsafe
            {
                if (values.Length > 0)
                {
                    fixed (double* p = &values[0])
                    {
                        Serialize(writer, p, values.Length);
                    }
                }
                else
                {
                    Serialize(writer, null, 0);
                }
            }
        }

        /// <summary>
        /// Serializes the specified <paramref name="values"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="values">The pointer to the array of values to serialize.</param>
        /// <param name="count">The number of elements in values array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Serialize(BinaryWriter writer, double* values, int count)
        {
            // Reserve one byte for future versioning.
            writer.Write((byte)1);
            SerializationUtils.WriteUInt32AsBase128(writer, (uint)count);
            if (count > 0)
            {
                BitBinaryWriter bitWriter = new BitBinaryWriter(writer);
                var previousState = new DoubleValueState(0, -1, -1);

                for (int i = 0; i < count; ++i)
                {
                    DoubleValueState newState;
                    WriteDouble(bitWriter, values[i], previousState, out newState);
                    previousState = newState;
                }

                bitWriter.Flush();
            }
        }

        /// <summary>
        /// Deserializes to an array of <see lang="double"/> from the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>An array of <see lang="double"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double[] Deserialize(BinaryReader reader)
        {
            // version not in use yet.
            reader.ReadByte();
            var numOfItems = (int)SerializationUtils.ReadUInt32FromBase128(reader);

            if (numOfItems == 0)
            {
                return EmptyDoubleArray;
            }

            var result = new double[numOfItems];

            unsafe
            {
                fixed (double* p = &result[0])
                {
                    DeserializeValues(reader, p, numOfItems);
                }
            }

            return result;
        }

        /// <summary>
        /// Deserializes to an array of <see lang="double"/> from the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="values">The pointer to the array to store the result values.</param>
        /// <param name="expectedCount">The number of elements in values array, this value should match the serialized array size.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Deserialize(BinaryReader reader, double* values, int expectedCount)
        {
            // version not in use yet.
            reader.ReadByte();
            var count = (int)SerializationUtils.ReadUInt32FromBase128(reader);

            if (count != expectedCount)
            {
                throw new InvalidDataException($"Wrong count in serialized data: expected {expectedCount}, but was {count}");
            }

            DeserializeValues(reader, values, count);
        }

        /// <summary>
        /// Deserializes to an array of nullable <see lang="double"/> from the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>An array of nullable <see lang="double"/>.</returns>
        public static double?[] DeserializeToNullableDoubles(BinaryReader reader)
        {
            // version not in use yet.
            reader.ReadByte();
            var numOfItems = (int)SerializationUtils.ReadUInt32FromBase128(reader);

            if (numOfItems == 0)
            {
                return EmptyNullableDoubleArray;
            }

            var result = new double?[numOfItems];

            var bitBinaryReader = new BitBinaryReader(reader);
            var previousState = new DoubleValueState(0, -1, -1);
            for (int i = 0; i < numOfItems; ++i)
            {
                var newState = ReadDouble(bitBinaryReader, previousState);
                result[i] = double.IsNaN(newState.Value) ? (double?)null : newState.Value;
                previousState = newState;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void DeserializeValues(BinaryReader reader, double* values, int count)
        {
            var bitBinaryReader = new BitBinaryReader(reader);
            var previousState = new DoubleValueState(0, -1, -1);
            for (int i = 0; i < count; ++i)
            {
                var newState = ReadDouble(bitBinaryReader, previousState);
                values[i] = newState.Value;
                previousState = newState;
            }
        }

        /// <summary>
        /// Encodes the given double value to the given writer.
        /// </summary>
        /// <param name="writer">The writer into which value needs to be encoded.</param>
        /// <param name="value">The value to be encoded.</param>
        /// <param name="previousValue">The previous value state.</param>
        /// <param name="newValue">The new value state.</param>
        /// <returns>True if encoding was success else false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool WriteDouble(BitBinaryWriter writer, double value, DoubleValueState previousValue, out DoubleValueState newValue)
        {
            newValue = previousValue;
            newValue.Value = value;

            var xor = BitConverter.DoubleToInt64Bits(value) ^ BitConverter.DoubleToInt64Bits(previousValue.Value);

            if (xor == 0)
            {
                writer.WriteBit(false);
            }
            else
            {
                writer.WriteBit(true);

                var leadingZeros = (sbyte)BitAggregateMagic.CountLeadingZeros(xor);
                var trailingZeros = (sbyte)BitAggregateMagic.CountTrailingZeros(xor);

                if (leadingZeros > MaxLeadingZerosLength)
                {
                    leadingZeros = MaxLeadingZerosLength;
                }

                int blockSize = 64 - leadingZeros - trailingZeros;
                int expectedSize = NumBitsToEncodeNumLeadingZeros + NumBitsToEncodeNumMeaningfulBits + blockSize;
                int previousBlockInformationSize = 64 - previousValue.TrailingZeros - previousValue.LeadingZeros;

                // The block position is set by the first non-zero XOR value. previousValue.LeadingZeros was initialized to -1s to start with.
                if (previousValue.LeadingZeros > 0 && leadingZeros >= previousValue.LeadingZeros && trailingZeros >= previousValue.TrailingZeros && previousBlockInformationSize < expectedSize)
                {
                    writer.WriteBit(false);

                    // there are at least as many leading zeros and as many trailing zeros as with the previous value, reuse the block position.
                    var numMeaningfulBits = BitAggregateMagic.NumBitsInLongInteger - previousValue.LeadingZeros - previousValue.TrailingZeros;

                    writer.WriteBits(xor, numMeaningfulBits, previousValue.TrailingZeros);
                }
                else
                {
                    // start a new block position
                    writer.WriteBit(true);

                    writer.WriteBits(leadingZeros, NumBitsToEncodeNumLeadingZeros, 0);

                    newValue.LeadingZeros = leadingZeros;

                    var numMeaningfulBits = BitAggregateMagic.NumBitsInLongInteger - leadingZeros - trailingZeros;
                    writer.WriteBits(numMeaningfulBits, NumBitsToEncodeNumMeaningfulBits, 0);

                    newValue.TrailingZeros = trailingZeros;

                    writer.WriteBits(xor, numMeaningfulBits, trailingZeros);
                }
            }

            return true;
        }

        /// <summary>
        /// Decodes the double value from the given reader.
        /// </summary>
        /// <param name="reader">The reader from which double value needs to be decoded.</param>
        /// <param name="state">The state of previous decoding.</param>
        /// <returns>Decoded double value with state.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DoubleValueState ReadDouble(BitBinaryReader reader, DoubleValueState state)
        {
            var firstBit = reader.ReadBit();
            if (!firstBit)
            {
                return state;
            }

            var secondBit = reader.ReadBit();
            long meaningFulBits;
            if (!secondBit)
            {
                var numBitsToRead = BitAggregateMagic.NumBitsInLongInteger - state.LeadingZeros - state.TrailingZeros;
                meaningFulBits = reader.ReadBits(numBitsToRead);
            }
            else
            {
                // a new block position was started since the number starts with "11".
                state.LeadingZeros = (sbyte)reader.ReadBits(NumBitsToEncodeNumLeadingZeros);
                var numBitsToRead = (sbyte)reader.ReadBits(NumBitsToEncodeNumMeaningfulBits);
                if (numBitsToRead == 0)
                {
                    // The block size is 64 bits which becomes 0 in writing into 6 bits - overflow.
                    // If the block size were indeed 0 bits, the xor value would be 0, and the actual value would be identical to the prior value,
                    // so we would have bailed out early on since firstBit would be 0.
                    numBitsToRead = (sbyte)BitAggregateMagic.NumBitsInLongInteger;
                }

                state.TrailingZeros = (sbyte)(BitAggregateMagic.NumBitsInLongInteger - state.LeadingZeros - numBitsToRead);
                meaningFulBits = reader.ReadBits(numBitsToRead);
            }

            var xor = meaningFulBits << state.TrailingZeros;
            state.Value = BitConverter.Int64BitsToDouble(xor ^ BitConverter.DoubleToInt64Bits(state.Value));
            return state;
        }

        private struct DoubleValueState
        {
            public double Value;
            public sbyte LeadingZeros;
            public sbyte TrailingZeros;

            public DoubleValueState(double value, sbyte leadingZeros, sbyte trailingZeros)
            {
                this.Value = value;
                this.LeadingZeros = leadingZeros;
                this.TrailingZeros = trailingZeros;
            }
        }
    }
}
