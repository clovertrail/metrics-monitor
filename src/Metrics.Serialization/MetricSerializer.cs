// -----------------------------------------------------------------------
// <copyright file="MetricSerializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Serializer used for serializing metric data between
    ///     - Metrics extension and FrontEnd
    ///     - Metric puller service and FrontEnd
    ///     - FrontEnd and metrics aggregator for cache server metrics
    /// </summary>
    public sealed class MetricSerializer
    {
        /// <summary>
        /// The maximum version that serializer can produce.
        /// To be used internally. The external code should
        /// always track their supported serializer/deserializer versions separately
        /// and be independent from maximum version supported by MetricSerializer
        /// </summary>
        private const ushort MaxVersion = 6;
        private const uint TypeSerializerFlags = 0x12020000; // Corresponds to 0001.001.0000.0001.00000000000000000 (Use string and metadata interning with variable-length integer serialization)
        private const uint TempBufferSize = 1500;
        private readonly Dictionary<string, uint> stringIndexes = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> strings = new List<string>();
        private readonly Dictionary<IMetricMetadata, uint> metadataIndexes = new Dictionary<IMetricMetadata, uint>(MetricEqualityComparer.Instance);
        private readonly List<IMetricMetadata> metadatas = new List<IMetricMetadata>();
        private readonly ushort hllSerializationVersion;
        private readonly bool estimatePacketSize;
        private readonly float stringCharEstimatedSizeInBytes;
        private ushort version;
        private uint nextStringIndex;
        private uint nextMetadataIndex;
        private long currentMetricDataBlockSize;
        private long currentMetadataDictionaryBlockSize;
        private long currentStringDictionaryBlockSize;
        private byte[] tempBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricSerializer"/> class.
        /// </summary>
        /// <param name="version">Serialization version to use (from 0 to MaxVersion).</param>
        /// <param name="hllSerializationVersion">Hll serialization version till whi</param>
        /// <param name="estimatePacketSize">Flag to know whether to do packet size estimate calculations as data is serialized.</param>
        /// <param name="stringCharEstimatedSizeInBytes">Estimated size in bytes for each character in strings used inside the packet.</param>
        public MetricSerializer(ushort version = 1, ushort hllSerializationVersion = 1, bool estimatePacketSize = false, float stringCharEstimatedSizeInBytes = 1.5f)
        {
            if (version > MaxVersion)
            {
                throw new ArgumentException("Version number is greated than maximum current version supported: " + MaxVersion, nameof(version));
            }

            if (stringCharEstimatedSizeInBytes > 2 || stringCharEstimatedSizeInBytes < 1)
            {
                throw new ArgumentException($"{nameof(stringCharEstimatedSizeInBytes)} cannot be greater than 2 and less than 1");
            }

            this.version = version;
            this.hllSerializationVersion = hllSerializationVersion;
            this.estimatePacketSize = estimatePacketSize;
            this.stringCharEstimatedSizeInBytes = stringCharEstimatedSizeInBytes;
        }

        /// <summary>
        /// Gets or sets the serialization version with which serializer is currently configured.
        /// </summary>
        public ushort SerializationVersion
        {
            get
            {
                return this.version;
            }

            set
            {
                if (value > MaxVersion)
                {
                    throw new ArgumentException("Version number is greated than maximum current version supported: " + MaxVersion, nameof(value));
                }

                this.version = value;
            }
        }

        /// <summary>
        /// Gets the expected size of the package in current serialization state.
        /// </summary>
        public long ExpectedPackageSize
        {
            get
            {
                if (this.estimatePacketSize)
                {
                    return this.currentMetricDataBlockSize + this.currentMetadataDictionaryBlockSize + this.currentStringDictionaryBlockSize +
                           SerializationUtils.EstimateUInt32InBase128Size((uint)this.strings.Count) +
                           SerializationUtils.EstimateUInt32InBase128Size((uint)this.metadatas.Count);
                }

                throw new InvalidOperationException("Packet size estimation is not enabled, use estimatePacketSize:true when creating the serializer");
            }
        }

        /// <summary>
        /// Serializes counter (metric) data to the stream.
        /// </summary>
        /// <param name="stream">Stream to which data should be serialized. Stream should be writable and provide random access.</param>
        /// <param name="metricData">Collection of metric data to be serialized.</param>
        public void Serialize(Stream stream, IEnumerable<IReadOnlyMetric> metricData)
        {
            if (!stream.CanWrite || !stream.CanSeek)
            {
                throw new ArgumentException("Stream should be writable and provide random access.", nameof(stream));
            }

            try
            {
                using (var writer = new NoCloseBinaryWriter(stream, Encoding.UTF8))
                {
                    var startStreamPosition = stream.Position;

                    // Write version and type serializers info
                    writer.Write(this.version);

                    long crcOffSet = 0;
                    long crcBodyOffSet = 0;
                    if (this.version >= 5)
                    {
                        // Add CRC
                        crcOffSet = stream.Position;
                        writer.Write((uint)0);
                        crcBodyOffSet = stream.Position;
                    }

                    writer.Write(TypeSerializerFlags);

                    // Reserve place to write type serializers data sections offsets
                    var offsetsPosition = stream.Position;
                    stream.Position += 2 * sizeof(long);

                    // Write metrics data
                    this.WriteMetricsData(writer, metricData);

                    // Write cached metrics metadata and offset
                    var serializerDataPosition = stream.Position;
                    stream.Position = offsetsPosition;
                    writer.Write(serializerDataPosition - startStreamPosition);
                    offsetsPosition = stream.Position;
                    stream.Position = serializerDataPosition;
                    SerializationUtils.WriteUInt32AsBase128(writer, (uint)this.metadatas.Count);
                    this.metadatas.ForEach(m => this.WriteMetricMetadata(writer, m));

                    // Write cached strings and offset
                    serializerDataPosition = stream.Position;
                    stream.Position = offsetsPosition;
                    writer.Write(serializerDataPosition - startStreamPosition);
                    stream.Position = serializerDataPosition;
                    SerializationUtils.WriteUInt32AsBase128(writer, (uint)this.strings.Count);
                    this.strings.ForEach(writer.Write);
                    var endOfStream = stream.Position;

                    if (this.version >= 5)
                    {
                        stream.Position = crcBodyOffSet;
                        var crc = Crc.ComputeCrc(0, stream, stream.Length - crcBodyOffSet);
                        stream.Position = crcOffSet;
                        writer.Write(crc);
                    }

                    stream.Position = endOfStream;
                }
            }
            catch (IOException ioException)
            {
                throw new MetricSerializationException("Failed to serialize data.", ioException);
            }
            finally
            {
                this.nextStringIndex = 0;
                this.stringIndexes.Clear();
                this.strings.Clear();
                this.nextMetadataIndex = 0;
                this.metadataIndexes.Clear();
                this.metadatas.Clear();
                this.currentMetricDataBlockSize = 0;
                this.currentMetadataDictionaryBlockSize = 0;
                this.currentStringDictionaryBlockSize = 0;
            }
        }

        private long EstimateStringSize(string value)
        {
            // Assuming 50 % characters are non-ascii
            return (int)(value.Length * this.stringCharEstimatedSizeInBytes) + SerializationUtils.EstimateUInt32InBase128Size((uint)value.Length);
        }

        private void WriteMetricsData(BinaryWriter writer, IEnumerable<IReadOnlyMetric> metricData)
        {
            Stream writerStream = writer.BaseStream;

            long currentTimeInMinutes = 0;
            if (this.version >= 5)
            {
                var currentTimeInTicks = DateTime.UtcNow.Ticks;
                currentTimeInMinutes = (currentTimeInTicks - (currentTimeInTicks % SerializationUtils.OneMinuteInterval)) / SerializationUtils.OneMinuteInterval;
                SerializationUtils.WriteUInt64AsBase128(writer, (ulong)currentTimeInMinutes);
            }

            var metricsCountPosition = writer.BaseStream.Position;
            writer.Write((uint)0);
            uint metricsCount = 0;
            foreach (var data in metricData)
            {
                ++metricsCount;
                var metadata = data.MetricMetadata;

                this.WriteMetricMetadataIndex(writer, metadata);

                // In versions 0-2 Monitoring Account and Metric Namespace was part of the Metric data
                // From version 3 Monitoring Account is removed and Metric Namespace became a part of Metric Metadata
                if (this.version < 3)
                {
                    SerializationUtils.WriteUInt32AsBase128(writer, this.RegisterString(data.MonitoringAccount));
                    SerializationUtils.WriteUInt32AsBase128(writer, this.RegisterString(data.MetricNamespace));
                }

                // In version 0 we had EventId, which was always passed as empty string
                if (this.version == 0)
                {
                    SerializationUtils.WriteUInt32AsBase128(writer, this.RegisterString(string.Empty));
                }

                if (this.version >= 5)
                {
                    var timeTicks = data.TimeUtc.Ticks;
                    long timeInMinutes = (timeTicks - (timeTicks % SerializationUtils.OneMinuteInterval)) / SerializationUtils.OneMinuteInterval;
                    SerializationUtils.WriteInt64AsBase128(writer, currentTimeInMinutes - timeInMinutes);
                }
                else
                {
                    SerializationUtils.WriteUInt64AsBase128(writer, (ulong)data.TimeUtc.Ticks);
                }

                for (byte j = 0; j < data.MetricMetadata.DimensionsCount; ++j)
                {
                    SerializationUtils.WriteUInt32AsBase128(writer, this.RegisterString(data.GetDimensionValue(j)));
                }

                var samplingTypes = data.SamplingTypes;
                bool useDouble = false;
                bool storeDoubleAsLong = false;
                if ((samplingTypes & SamplingTypes.DoubleValueType) != 0)
                {
                    useDouble = true;

                    if (data.SumUnion.CanRepresentDoubleAsLong() &&
                        ((samplingTypes & SamplingTypes.Min) == 0 ||
                         (data.MinUnion.CanRepresentDoubleAsLong() && data.MaxUnion.CanRepresentDoubleAsLong())))
                    {
                        samplingTypes = samplingTypes | SamplingTypes.DoubleValueStoredAsLongType;
                        storeDoubleAsLong = true;
                    }
                }

                SerializationUtils.WriteUInt32AsBase128(writer, (uint)samplingTypes);

                if ((data.SamplingTypes & SamplingTypes.Min) != 0)
                {
                    this.WriteMetricValue(data.MinUnion, useDouble, storeDoubleAsLong, writer);
                }

                if ((data.SamplingTypes & SamplingTypes.Max) != 0)
                {
                    this.WriteMetricValue(data.MaxUnion, useDouble, storeDoubleAsLong, writer);
                }

                if ((data.SamplingTypes & SamplingTypes.Sum) != 0)
                {
                    this.WriteMetricValue(data.SumUnion, useDouble, storeDoubleAsLong, writer);
                }

                if ((data.SamplingTypes & SamplingTypes.Count) != 0)
                {
                    SerializationUtils.WriteUInt32AsBase128(writer, data.Count);
                }

                if ((data.SamplingTypes & SamplingTypes.SumOfSquareDiffFromMean) != 0)
                {
                    writer.Write(data.SumOfSquareDiffFromMean);
                }

                if ((data.SamplingTypes & SamplingTypes.Histogram) != 0)
                {
                    if (data.Histogram == null)
                    {
                        var message = string.Format(
                            CultureInfo.InvariantCulture,
                            "Invalid input data. Declared sampling type contains Histogram, but Histogram data is null. Metric:({0},{1},{2}).",
                            data.MonitoringAccount,
                            data.MetricNamespace,
                            data.MetricMetadata.MetricName);

                        throw new MetricSerializationException(message, null);
                    }

                    SerializationUtils.WriteHistogramDataHistogram(writer, data.Histogram.SamplesCount, data.Histogram.Samples, this.version > 3);
                }

                if ((data.SamplingTypes & SamplingTypes.HyperLogLogSketch) != 0)
                {
                    if (data.HyperLogLogSketchesStream == null && data.HyperLogLogSketches == null)
                    {
                        var message = string.Format(
                            CultureInfo.InvariantCulture,
                            "Invalid input data. Declared sampling type contains sketches, but sketches data is null. Metric:({0},{1},{2}).",
                            data.MonitoringAccount,
                            data.MetricNamespace,
                            data.MetricMetadata.MetricName);
                        throw new MetricSerializationException(message, null);
                    }

                    if (data.HyperLogLogSketchesStream != null)
                    {
                        if (this.tempBuffer == null)
                        {
                            this.tempBuffer = new byte[TempBufferSize];
                        }

                        writer.Write((int)data.HyperLogLogSketchesStream.Length);
                        var sketchStreamStartPosition = data.HyperLogLogSketchesStream.Position;
                        SerializationUtils.ReadFromStream(data.HyperLogLogSketchesStream, writer.BaseStream, (int)data.HyperLogLogSketchesStream.Length, this.tempBuffer);
                        data.HyperLogLogSketchesStream.Position = sketchStreamStartPosition;
                    }
                    else
                    {
                        if (this.hllSerializationVersion == 0)
                        {
                            SerializationUtils.WriteHyperLogLogSketches(writer, data.HyperLogLogSketches.HyperLogLogSketchesCount, data.HyperLogLogSketches.HyperLogLogSketches);
                        }
                        else
                        {
                            SerializationUtils.WriteHyperLogLogSketchesV2(writer, data.HyperLogLogSketches.HyperLogLogSketchesCount, data.HyperLogLogSketches.HyperLogLogSketches);
                        }
                    }
                }

                if (this.version >= 6)
                {
                    if ((data.SamplingTypes & samplingTypes & SamplingTypes.TDigest) != 0)
                    {
                        SerializationUtils.WriteUInt32AsBase128(writer, FrontEndMetricDeserializer<IMetricMetadata>.TDigestPrefixValue);

                        long pos = writerStream.Position;

                        // placeholder for length encoded as 4 bytes
                        writer.Write((ushort)0);
                        writer.Write((ushort)0);

                        data.TDigest.Serialize(writer);
                        long tdigestSerializedLength = writerStream.Position - pos - 4;
                        if (tdigestSerializedLength > ushort.MaxValue)
                        {
                            throw new ArgumentException("TDigest too big");
                        }

                        writerStream.Position = pos;
                        SerializationUtils.WriteUInt32InBase128AsFixed4Bytes(writer, (ushort)tdigestSerializedLength);

                        writerStream.Position += tdigestSerializedLength;
                    }

                    SerializationUtils.WriteUInt32AsBase128(writer, 0);
                }

                this.currentMetricDataBlockSize = writer.BaseStream.Position;
            }

            var currentPosition = writer.BaseStream.Position;
            writer.BaseStream.Position = metricsCountPosition;

            // Versions before 2 used variable number of bytes to write number of serialized metrics data.
            // From version 2 passing IEnumerable<IReadOnlyMetric> is supported, thus number of metrics data
            // is unknown beforehand and we cannot use variable number anymore. Thus we use fixed 4 bytes
            // uint. To keep compatibility with previous versions, while still have ability to serialize
            // variable amount of data, we write uint number in variable manner but with fixed number of bytes.
            if (this.version >= 2)
            {
                writer.Write(metricsCount);
            }
            else
            {
                SerializationUtils.WriteUInt32InBase128AsFixed4Bytes(writer, metricsCount);
            }

            writer.BaseStream.Position = currentPosition;
        }

        private void WriteMetricMetadataIndex(BinaryWriter writer, IMetricMetadata value)
        {
            uint index;
            if (!this.metadataIndexes.TryGetValue(value, out index))
            {
                index = this.nextMetadataIndex++;
                this.metadataIndexes.Add(value, index);
                this.metadatas.Add(value);

                if (this.estimatePacketSize)
                {
                    // In versions 0-2 Metric Namespace was part of the Metric data, from version 3 it became a part of Metric Metadata
                    if (this.version >= 3)
                    {
                        this.currentMetadataDictionaryBlockSize += SerializationUtils.EstimateUInt32InBase128Size(this.RegisterString(value.MetricNamespace));
                    }

                    this.currentMetadataDictionaryBlockSize += SerializationUtils.EstimateUInt32InBase128Size(this.RegisterString(value.MetricName));
                    this.currentMetadataDictionaryBlockSize += SerializationUtils.EstimateUInt32InBase128Size((uint)value.DimensionsCount);
                    for (int i = 0; i < value.DimensionsCount; ++i)
                    {
                        this.currentMetadataDictionaryBlockSize += SerializationUtils.EstimateUInt32InBase128Size(this.RegisterString(value.GetDimensionName(i)));
                    }
                }
            }

            SerializationUtils.WriteUInt32AsBase128(writer, index);
        }

        private void WriteMetricMetadata(BinaryWriter writer, IMetricMetadata value)
        {
            // In versions 0-2 Metric Namespace was part of the Metric data, from version 3 it became a part of Metric Metadata
            if (this.version >= 3)
            {
                SerializationUtils.WriteUInt32AsBase128(writer, this.RegisterString(value.MetricNamespace));
            }

            SerializationUtils.WriteUInt32AsBase128(writer, this.RegisterString(value.MetricName));
            SerializationUtils.WriteUInt32AsBase128(writer, (uint)value.DimensionsCount);
            for (var i = 0; i < value.DimensionsCount; ++i)
            {
                SerializationUtils.WriteUInt32AsBase128(writer, this.RegisterString(value.GetDimensionName(i)));
            }
        }

        private void WriteMetricValue(MetricValueV2 value, bool useDouble, bool storeDoubleAsLong, BinaryWriter writer)
        {
            if (useDouble)
            {
                if (storeDoubleAsLong)
                {
                    SerializationUtils.WriteInt64AsBase128(writer, (long)value.ValueAsDouble);
                }
                else
                {
                    writer.Write(value.ValueAsDouble);
                }
            }
            else
            {
                SerializationUtils.WriteUInt64AsBase128(writer, value.ValueAsULong);
            }
        }

        private uint RegisterString(string value)
        {
            uint index;
            value = value ?? string.Empty;
            if (!this.stringIndexes.TryGetValue(value, out index))
            {
                index = this.nextStringIndex++;
                this.stringIndexes.Add(value, index);
                this.strings.Add(value);

                if (this.estimatePacketSize)
                {
                    this.currentStringDictionaryBlockSize += this.EstimateStringSize(value);
                }
            }

            return index;
        }

        private sealed class MetricEqualityComparer : IEqualityComparer<IMetricMetadata>
        {
            public static readonly MetricEqualityComparer Instance = new MetricEqualityComparer();

            public bool Equals(IMetricMetadata x, IMetricMetadata y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x.DimensionsCount != y.DimensionsCount ||
                    !StringComparer.OrdinalIgnoreCase.Equals(x.MetricNamespace, y.MetricNamespace) ||
                    !StringComparer.OrdinalIgnoreCase.Equals(x.MetricName, y.MetricName))
                {
                    return false;
                }

                for (int i = 0; i < x.DimensionsCount; ++i)
                {
                    if (!StringComparer.OrdinalIgnoreCase.Equals(x.GetDimensionName(i), y.GetDimensionName(i)))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(IMetricMetadata obj)
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.MetricName) ^
                       StringComparer.OrdinalIgnoreCase.GetHashCode(obj.MetricNamespace);
            }
        }

        private sealed class NoCloseBinaryWriter : BinaryWriter
        {
            public NoCloseBinaryWriter(Stream stream, Encoding encoding)
                : base(stream, encoding)
            {
            }

            protected override void Dispose(bool disposing)
            {
                this.BaseStream.Flush();
                base.Dispose(false);
            }
        }
    }
}
