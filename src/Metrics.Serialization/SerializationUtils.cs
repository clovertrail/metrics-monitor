// -----------------------------------------------------------------------
// <copyright file="SerializationUtils.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Set of functions used for metrics serialization and deserialization.
    /// </summary>
    public static class SerializationUtils
    {
        /// <summary>
        /// A string which replaces empty dimension value.
        /// </summary>
        public const string EmptyDimensionValueString = "__Empty";

        /// <summary>
        /// One minute size in 100th nanoseconds.
        /// </summary>
        public const long OneMinuteInterval = 600000000;

        /// <summary>
        /// Writes UInt16 value to the stream.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to write to.</param>
        /// <param name="value">Value to write.</param>
        public static void Write(Stream stream, ushort value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value & 0xFF00) >> 8));
        }

        /// <summary>
        /// Writes int value Base-128 encoded.
        /// </summary>
        /// <param name="writer">Binary writer to be used.</param>
        /// <param name="value">Value to write.</param>
        public static void WriteInt32AsBase128(BinaryWriter writer, int value)
        {
            WriteInt64AsBase128(writer, value);
        }

        /// <summary>
        /// Writes long value Base-128 encoded.
        /// </summary>
        /// <param name="writer">Binary writer to be used.</param>
        /// <param name="value">Value to write.</param>
        public static void WriteInt64AsBase128(BinaryWriter writer, long value)
        {
            var negative = value < 0;
            var t = negative ? -value : value;
            var first = true;
            do
            {
                byte b;
                if (first)
                {
                    b = (byte)(t & 0x3f);
                    t >>= 6;
                    if (negative)
                    {
                        b = (byte)(b | 0x40);
                    }

                    first = false;
                }
                else
                {
                    b = (byte)(t & 0x7f);
                    t >>= 7;
                }

                if (t > 0)
                {
                    b |= 0x80;
                }

                writer.Write(b);
            }
            while (t > 0);
        }

        /// <summary>
        /// Writes uint value Base-128 encoded.
        /// </summary>
        /// <param name="writer">Binary writer to be used.</param>
        /// <param name="value">Value to write.</param>
        public static void WriteUInt32AsBase128(BinaryWriter writer, uint value)
        {
            WriteUInt64AsBase128(writer, value);
        }

        /// <summary>
        /// Writes uint value as 4 bytes, but Base-128 encoded.
        /// Used for back compatibility between some versions of serializer.
        /// </summary>
        /// <param name="writer">Binary writer to be used.</param>
        /// <param name="value">Value to write.</param>
        public static void WriteUInt32InBase128AsFixed4Bytes(BinaryWriter writer, uint value)
        {
            var count = 0;
            ulong t = value;
            do
            {
                var b = (byte)(t & 0x7f);
                t >>= 7;
                ++count;
                if (t > 0 || count < 4)
                {
                    b |= 0x80;
                }

                writer.Write(b);
            }
            while (t > 0);

            for (; ++count <= 4;)
            {
                writer.Write(count == 4 ? (byte)0x0 : (byte)0x80);
            }
        }

        /// <summary>
        /// Writes ulong value Base-128 encoded.
        /// </summary>
        /// <param name="writer">Binary writer to be used.</param>
        /// <param name="value">Value to write.</param>
        public static void WriteUInt64AsBase128(BinaryWriter writer, ulong value)
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

                writer.Write(b);
            }
            while (t > 0);
        }

        /// <summary>
        /// Writes ulong value Base-128 encoded to the buffer starting from the specified offset.
        /// </summary>
        /// <param name="buffer">Buffer used for writing.</param>
        /// <param name="offset">Offset to start with. Will be moved to the next byte after written.</param>
        /// <param name="value">Value to write.</param>
        public static void WriteUInt64AsBase128(byte[] buffer, ref int offset, ulong value)
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

                buffer[offset++] = b;
            }
            while (t > 0);
        }

        /// <summary>
        /// Writes a histogram.
        /// </summary>
        /// <param name="writer">Binary writer to be used.</param>
        /// <param name="bucketsCount">The number of buckets in histogram (must be equal to the number of elements in buckets collection).</param>
        /// <param name="buckets">Histogram buckets to write.</param>
        /// <param name="hasHistogramSizePrefix">A flag indicating whether histogram size (in bytes)
        ///     should be put as a 4-bytes prefix in the beginning of the histogram data.</param>
        public static void WriteHistogramDataHistogram(BinaryWriter writer, int bucketsCount, IEnumerable<KeyValuePair<ulong, uint>> buckets, bool hasHistogramSizePrefix = false)
        {
            var startPosition = writer.BaseStream.Position;
            if (hasHistogramSizePrefix)
            {
                writer.Write(0);
            }

            WriteUInt32AsBase128(writer, (uint)bucketsCount);

            int bucketsWritten = 0;
            ulong prevKey = 0;
            uint prevValue = 0;
            var firstTime = true;
            foreach (var bucket in buckets)
            {
                if (firstTime)
                {
                    prevKey = bucket.Key;
                    prevValue = bucket.Value;
                    WriteUInt64AsBase128(writer, prevKey);
                    WriteUInt32AsBase128(writer, prevValue);
                    firstTime = false;
                }
                else
                {
                    WriteUInt64AsBase128(writer, bucket.Key - prevKey);
                    WriteInt32AsBase128(writer, (int)bucket.Value - (int)prevValue);
                    prevKey = bucket.Key;
                    prevValue = bucket.Value;
                }

                ++bucketsWritten;
            }

            if (bucketsCount != bucketsWritten)
            {
                throw new ArgumentException(
                    $"The actual number of buckets in the {nameof(buckets)} was {bucketsWritten}, the passed {nameof(bucketsCount)} is {bucketsCount}",
                    nameof(bucketsCount));
            }

            if (hasHistogramSizePrefix)
            {
                var endPosition = writer.BaseStream.Position;
                writer.BaseStream.Position = startPosition;
                writer.Write((int)(endPosition - startPosition - sizeof(int)));
                writer.BaseStream.Position = endPosition;
            }
        }

        /// <summary>
        /// Writes collection of <see cref="HyperLogLogSketch"/> objects to a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer"><see cref="BinaryWriter"/> object used for writing.</param>
        /// <param name="count">Number of sketches to write.</param>
        /// <param name="data">Collection of sketch dimension - sketch value pairs.</param>
        public static void WriteHyperLogLogSketches(BinaryWriter writer, uint count, IEnumerable<KeyValuePair<string, HyperLogLogSketch>> data)
        {
            var startPosition = writer.BaseStream.Position;
            writer.Write(0);

            WriteUInt32AsBase128(writer, count);

            foreach (var sketch in data)
            {
                writer.Write(sketch.Key);
                writer.Write(sketch.Value.BValue);
                writer.Write(sketch.Value.Registers, 0, sketch.Value.Registers.Length);
            }

            var endPosition = writer.BaseStream.Position;
            writer.BaseStream.Position = startPosition;
            writer.Write((int)(endPosition - startPosition - sizeof(int)));
            writer.BaseStream.Position = endPosition;
        }

        /// <summary>
        /// Writes collection of <see cref="HyperLogLogSketch"/> objects to a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer"><see cref="BinaryWriter"/> object used for writing.</param>
        /// <param name="count">Number of sketches to write.</param>
        /// <param name="data">Collection of sketch dimension - sketch value pairs.</param>
        /// <remarks>
        /// Will be replaced by V1 once deserialization code reaches everywhere.
        ///
        /// HLL data is serialized in two format:
        /// Sparse HLL data format
        ///     Version = 2 as byte
        ///     BValue as byte
        ///     For all non-zero elements :
        ///         Position In Sketch(ushort), Value at that position(byte)
        /// Full HLL data format
        ///     Version = 1 as byte
        ///     BValue as byte
        ///     For all elements :
        ///         Value at that position(byte)
        /// </remarks>
        public static void WriteHyperLogLogSketchesV2(BinaryWriter writer, uint count, IEnumerable<KeyValuePair<string, HyperLogLogSketch>> data)
        {
            var startPosition = writer.BaseStream.Position;
            writer.Write(0);

            WriteUInt32AsBase128(writer, count);

            foreach (var sketch in data)
            {
                writer.Write(sketch.Key);

                ushort nonZeroElements = 0;
                for (int i = 0; i < sketch.Value.Registers.Length; i++)
                {
                    if (sketch.Value.Registers[i] > 0)
                    {
                        nonZeroElements++;
                    }
                }

                // Position is 2 bytes, value is 1 byte, length of nonzero bytes is 2 bytes
                if ((nonZeroElements * 2) + nonZeroElements + 2 < sketch.Value.Registers.Length)
                {
                    // Sparse HLL
                    writer.Write((byte)2);
                    writer.Write(sketch.Value.BValue);
                    writer.Write(nonZeroElements);
                    for (ushort i = 0; i < sketch.Value.Registers.Length; i++)
                    {
                        if (sketch.Value.Registers[i] > 0)
                        {
                            writer.Write(i);
                            writer.Write(sketch.Value.Registers[i]);
                        }
                    }
                }
                else
                {
                    // Non Sparse HLL
                    writer.Write((byte)1);
                    writer.Write(sketch.Value.BValue);
                    writer.Write(sketch.Value.Registers, 0, sketch.Value.Registers.Length);
                }
            }

            var endPosition = writer.BaseStream.Position;
            writer.BaseStream.Position = startPosition;
            writer.Write((int)(endPosition - startPosition - sizeof(int)));
            writer.BaseStream.Position = endPosition;
        }

        /// <summary>
        /// Estimates a size in bytes of the uint value when written in Base-128.
        /// </summary>
        /// <param name="value">Value which size has to be estimated.</param>
        /// <returns>The estimated size in bytes.</returns>
        public static long EstimateUInt32InBase128Size(uint value)
        {
            return value <= 0x7F ? 1 : (value < 0x91011 ? 2 : (value < 0x1FFFFF ? 3 : 4));
        }

        /// <summary>
        /// Reads int value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <returns>Read value.</returns>
        public static int ReadInt32FromBase128(BinaryReader reader)
        {
            return (int)ReadInt64FromBase128(reader);
        }

        /// <summary>
        /// Reads long value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64FromBase128(BinaryReader reader)
        {
            int dummy = 0;
            return ReadInt64FromBase128(reader, ref dummy);
        }

        /// <summary>
        /// Reads long value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <param name="bytesRead">The number that is incremented each time a single byte is read.</param>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64FromBase128(BinaryReader reader, ref int bytesRead)
        {
            long val = 0;
            var shift = 0;
            byte b;
            var first = true;
            var negative = false;
            do
            {
                if (first)
                {
                    first = false;
                    b = reader.ReadByte();
                    bytesRead++;
                    val += (b & 0x3f) << shift;
                    negative = (b & 0x40) != 0;
                    shift += 6;
                }
                else
                {
                    b = reader.ReadByte();
                    bytesRead++;
                    val += (long)(b & 0x7f) << shift;
                    shift += 7;
                }
            }
            while ((b & 0x80) != 0);
            return negative ? -val : val;
        }

        /// <summary>
        /// Reads uint value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <returns>Read value.</returns>
        public static uint ReadUInt32FromBase128(BinaryReader reader)
        {
            return (uint)ReadUInt64FromBase128(reader);
        }

        /// <summary>
        /// Reads uint value stored in Base-128 encoding.
        /// </summary>
        /// <param name="buffer">Buffer from which value to be read.</param>
        /// <param name="offset">Offset in buffer to start reading from.</param>
        /// <returns>Read value.</returns>
        public static uint ReadUInt32FromBase128(byte[] buffer, ref int offset)
        {
            return (uint)ReadUInt64FromBase128(buffer, ref offset);
        }

        /// <summary>
        /// Reads ulong value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <returns>Read value.</returns>
        public static ulong ReadUInt64FromBase128(BinaryReader reader)
        {
            ulong val = 0;
            var shift = 0;
            byte b;
            do
            {
                b = reader.ReadByte();
                val = val + ((ulong)(b & 0x7f) << shift);
                shift += 7;
            }
            while ((b & 0x80) != 0);
            return val;
        }

        /// <summary>
        /// Reads ulong value stored in Base-128 encoding.
        /// </summary>
        /// <param name="buffer">Buffer from which value to be read.</param>
        /// <param name="offset">Offset in buffer to start reading from.</param>
        /// <returns>Read value.</returns>
        public static ulong ReadUInt64FromBase128(byte[] buffer, ref int offset)
        {
            ulong val = 0;
            var shift = 0;
            byte b;
            do
            {
                b = buffer[offset++];
                val = val + ((ulong)(b & 0x7f) << shift);
                shift += 7;
            }
            while ((b & 0x80) != 0);
            return val;
        }

        /// <summary>
        /// Reads a histogram.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <param name="hasHistogramSizePrefix">A flag indicating whether histogram
        /// data contains histogram size (in bytes) as a 4-bytes prefix.</param>
        /// <returns>An enumerable of key values pairs where key is a sample value
        /// and value is a number of times the sample value appeared.</returns>
        public static IEnumerable<KeyValuePair<ulong, uint>> ReadHistogram(BinaryReader reader, bool hasHistogramSizePrefix = false)
        {
            if (hasHistogramSizePrefix)
            {
                // Not used now, but left for optimization later
                reader.ReadInt32();
            }

            var size = ReadUInt32FromBase128(reader);
            ulong prevKey = 0;
            uint prevValue = 0;
            for (var i = 0; i < size; ++i)
            {
                if (i == 0)
                {
                    prevKey = ReadUInt64FromBase128(reader);
                    prevValue = ReadUInt32FromBase128(reader);
                }
                else
                {
                    prevKey += ReadUInt64FromBase128(reader);
                    prevValue = (uint)(prevValue + ReadInt32FromBase128(reader));
                }

                yield return new KeyValuePair<ulong, uint>(prevKey, prevValue);
            }
        }

        /// <summary>
        /// Reads a histogram to a list.
        /// </summary>
        /// <param name="list">List to append to</param>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <param name="hasHistogramSizePrefix">A flag indicating whether histogram
        /// data contains histogram size (in bytes) as a 4-bytes prefix.</param>
        public static void ReadHistogramTo(List<KeyValuePair<ulong, uint>> list, BinaryReader reader, bool hasHistogramSizePrefix = false)
        {
            list.Clear();

            if (hasHistogramSizePrefix)
            {
                // Not used now, but left for optimization later
                reader.ReadInt32();
            }

            var size = ReadUInt32FromBase128(reader);
            ulong prevKey = 0;
            uint prevValue = 0;

            list.EnsureSpace((int)size, geometryGrowth: true);

            for (var i = 0; i < size; ++i)
            {
                if (i == 0)
                {
                    prevKey = ReadUInt64FromBase128(reader);
                    prevValue = ReadUInt32FromBase128(reader);
                }
                else
                {
                    prevKey += ReadUInt64FromBase128(reader);
                    prevValue = (uint)(prevValue + ReadInt32FromBase128(reader));
                }

                list.Add(new KeyValuePair<ulong, uint>(prevKey, prevValue));
            }
        }

        /// <summary>
        /// Assigns the hyperloglog sketches.
        /// </summary>
        /// <param name="reader">Stream reader containing the sketches data.</param>
        /// <param name="length">Length of data to read.</param>
        /// <param name="sketchConstructor">Function used to construct <see cref="HyperLogLogSketch"/> object.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of dimension name and hyperloglog sketch key value pair.</returns>
        public static IEnumerable<KeyValuePair<string, HyperLogLogSketch>> ReadHyperLogLogSketch(BinaryReader reader, int length, Func<int, HyperLogLogSketch> sketchConstructor)
        {
            var size = ReadUInt32FromBase128(reader);
            for (var i = 0; i < size; ++i)
            {
                var dimensionName = reader.ReadString();
                short bValue;
                var sketch = ReadHyperLogLogSketch(reader, sketchConstructor, out bValue);
                yield return new KeyValuePair<string, HyperLogLogSketch>(dimensionName, sketch);
            }
        }

        /// <summary>
        /// Reads the hyperloglog sketch.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="sketchConstructor">The sketch constructor.</param>
        /// <param name="bValue">B value.</param>
        /// <returns>Deserialized sketch.</returns>
        /// <remarks>
        /// HLL data is serialized in three formats:
        /// Legacy HLL data format
        ///     BValue as byte
        ///     For all elements :
        ///         Value at that position(byte)
        /// Sparse HLL data format
        ///     Version = 2 as byte
        ///     BValue as byte
        ///     For all non-zero elements :
        ///         Position In Sketch(ushort), Value at that position(byte)
        /// Full HLL data format
        ///     Version = 1 as byte
        ///     BValue as byte
        ///     For all elements :
        ///         Value at that position(byte)
        /// </remarks>
        public static HyperLogLogSketch ReadHyperLogLogSketch(BinaryReader reader, Func<int, HyperLogLogSketch> sketchConstructor, out short bValue)
        {
            var version = (short)reader.ReadByte();
            if (version > 3)
            {
                // Legacy format when no version was present
                bValue = version;
                version = 0;
            }
            else
            {
                // All other formats has bValue as next element
                bValue = reader.ReadByte();
            }

            var sketch = sketchConstructor(bValue);

            if (version <= 1)
            {
                // Non-Sparse HLL
                var registersSize = 1 << bValue;
                for (var j = 0; j < registersSize; j++)
                {
                    sketch[j] = reader.ReadByte();
                }
            }
            else if (version == 2)
            {
                // Sparse HLL, hence format is position of nonzero items,value of nonzeroitem
                var lengthOfNonZeroElements = reader.ReadUInt16();
                for (var pos = 0; pos < lengthOfNonZeroElements; pos++)
                {
                    var itemPosition = reader.ReadUInt16();
                    sketch[itemPosition] = reader.ReadByte();
                }
            }

            return sketch;
        }

        /// <summary>
        /// Reads data from source stream and writes it to destination stream.
        /// </summary>
        /// <param name="source">Source stream.</param>
        /// <param name="destination">Destination stream.</param>
        /// <param name="len">Length to copy.</param>
        /// <param name="tempBuffer">Temp buffer.</param>
        public static void ReadFromStream(Stream source, Stream destination, int len, byte[] tempBuffer)
        {
            var readBytes = 0;
            while (readBytes < len)
            {
                var bytesToRead = Math.Min(tempBuffer.Length, len - readBytes);
                source.Read(tempBuffer, 0, bytesToRead);
                destination.Write(tempBuffer, 0, bytesToRead);
                readBytes += bytesToRead;
            }
        }

        /// <summary>
        /// Ensure List has enough capacity for new addition to reduce resizing
        /// </summary>
        /// <typeparam name="T">Generic type</typeparam>
        /// <param name="list">Existing list</param>
        /// <param name="addCount">Estimated number of items to add</param>
        /// <param name="geometryGrowth">True for geometry growth (50% more, O(N) algorithm),
        /// False for no extra capacity (could lead to O(N^2) algorithm, use when memory is critical and this is not called often.).</param>
        private static void EnsureSpace<T>(this List<T> list, int addCount, bool geometryGrowth = true)
        {
            if (addCount > 0)
            {
                int cap = list.Capacity;

                if (cap == 0)
                {
                    // New list, use toAdd
                    cap = addCount;
                }
                else
                {
                    int newCount = list.Count + addCount;

                    // List has enough capacity
                    if (newCount <= cap)
                    {
                        return;
                    }

                    if (geometryGrowth)
                    {
                        // Geometry growth (add 50%)
                        cap = Math.Max(newCount, cap * 3 / 2);
                    }
                    else
                    {
                        cap = newCount;
                    }
                }

                list.Capacity = cap;
            }
        }
    }
}
