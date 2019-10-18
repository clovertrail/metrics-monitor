// -----------------------------------------------------------------------
// <copyright file="FrontEndMetricDeserializer.cs" company="Microsoft Corporation">
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
    /// Class which deserializes Autopilot count (metric) data.
    /// Serialization format corresponds to one described in http://sharepoint/sites/autopilot/team/Docs/Silicon/Monitroing%20Team/AD%20Metrics%20%20Autopilot%20Counter%20Data%20Common%20Serialization%20Format.docx.
    /// </summary>
    /// <typeparam name="TMetadata">Type of metadata object used for deserialization.</typeparam>
    public sealed class FrontEndMetricDeserializer<TMetadata>
        where TMetadata : IMetricMetadata
    {
        /// <summary>
        /// TDigest format prefix value
        /// </summary>
        public const uint TDigestPrefixValue = 0x74; // == 't' "tDigest"

        private const ushort MinVersion = 3;
        private const ushort MaxVersion = 6;
        private const uint TypeSerializerFlags = 0x12020000; // Corresponds to 0001.001.0000.0001.00000000000000000 (Use string and metadata interning with variable-length integer serialization)
        private readonly List<string> stringsDictionary = new List<string>();
        private readonly List<TMetadata> metadataDictionary = new List<TMetadata>();
        private readonly List<KeyValuePair<ulong, uint>> histogramBuffer = new List<KeyValuePair<ulong, uint>>(2000);
        private readonly List<string> reusableStringsList = new List<string>();

        /// <summary>
        /// Validates the data packet by CRC check
        /// </summary>
        /// <param name="dataPacket">Data packet to check.</param>
        /// <exception cref="CrcCheckFailedSerializationException">
        /// Throws when CRC check fails.
        /// </exception>
        public static void ValidateCrc(byte[] dataPacket)
        {
            var version = (ushort)(dataPacket[0] | dataPacket[1] << 8);

            if (version < 5)
            {
                // No CRC is added for versions less than 5.
                return;
            }

            var crc = (uint)(dataPacket[2] | dataPacket[3] << 8 | dataPacket[4] << 16 | dataPacket[5] << 24);
            var computedCrc = Crc.ComputeCrc(0, dataPacket, 6);

            if (crc != computedCrc)
            {
                throw new CrcCheckFailedSerializationException($"Crc check failed. Computed CRC : {crc}, Packet CRC: {computedCrc}");
            }
        }

        /// <summary>
        /// Deserializes counter (metric) data from the stream and adds all objects to provided collection.
        /// </summary>
        /// <param name="stream">Stream from which data should be deserialized. Stream should be readable and provide randon access.</param>
        /// <param name="metricBuilder">An object responsible for creation and further consumption of deserialized data.</param>
        /// <param name="maxMetricStringsLength">Maximum length of strings, which represent metric name, dimension names and values.</param>
        /// <param name="maxMetricNamespaceStringsLength">Maximum length of metric namespace string.</param>
        /// <param name="maxMetricDimensionValueStringsLength">Maximum length of metric dimension value string.</param>
        public void Deserialize(
            Stream stream,
            IFrontEndMetricBuilder<TMetadata> metricBuilder,
            int maxMetricStringsLength,
            int maxMetricNamespaceStringsLength,
            int maxMetricDimensionValueStringsLength)
        {
            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException(@"Stream should be readable and provide random access.", nameof(stream));
            }

            try
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    var startStreamPosition = stream.Position;

                    // Read version and type serializers info
                    var version = reader.ReadUInt16();
                    if (version < MinVersion || version > MaxVersion)
                    {
                        throw new VersionNotSupportedMetricSerializationException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Version is not supported. ReadVersion:{0}, MinVersion:{1}, MaxVersion:{2}.",
                                version,
                                MinVersion,
                                MaxVersion));
                    }

                    if (version >= 5)
                    {
                        // Read CRC. CRC check is done in upper layers.
                        reader.ReadUInt32();
                    }

                    if (reader.ReadUInt32() != TypeSerializerFlags)
                    {
                        throw new VersionNotSupportedMetricSerializationException("Type serializers not supported.");
                    }

                    metricBuilder.SetSerializationVersion(version);

                    // Read strings
                    var deserializerDataPosition = stream.Position;
                    stream.Position += sizeof(long);
                    stream.Position = startStreamPosition + reader.ReadInt64();
                    var count = SerializationUtils.ReadUInt32FromBase128(reader);
                    for (uint i = 0; i < count; ++i)
                    {
                        this.stringsDictionary.Add(metricBuilder.GetString(reader.ReadString()));
                    }

                    var endOfPacketStreamPosition = stream.Position;

                    // Read metrics metadata
                    stream.Position = deserializerDataPosition;
                    stream.Position = startStreamPosition + reader.ReadInt64();
                    count = SerializationUtils.ReadUInt32FromBase128(reader);
                    for (uint i = 0; i < count; ++i)
                    {
                        this.metadataDictionary.Add(this.ReadMetricMetadata(reader, metricBuilder, maxMetricStringsLength, maxMetricNamespaceStringsLength));
                    }

                    // Read metrics data
                    stream.Position = deserializerDataPosition + (2 * sizeof(long));
                    this.ReadMetricsData(reader, metricBuilder, version, maxMetricDimensionValueStringsLength);

                    // Bring back the stream to total read data position
                    stream.Position = endOfPacketStreamPosition;
                }
            }
            catch (IOException ioException)
            {
                throw new MetricSerializationException("Failed to deserialize data. Problem with input stream.", ioException);
            }
            catch (Exception exception)
            {
                var serializationException = exception as MetricSerializationException;
                bool isInvalidData = false;
                if (serializationException != null)
                {
                    isInvalidData = serializationException.IsInvalidData;
                }

                throw new MetricSerializationException("Failed to deserialize data. Likely the incoming stream contains corrupted data.", exception, isInvalidData);
            }
            finally
            {
                this.metadataDictionary.Clear();
                this.stringsDictionary.Clear();
                this.histogramBuffer.Clear();
                this.reusableStringsList.Clear();
            }
        }

        /// <summary>
        /// Read metrics count and advances the stream.
        /// </summary>
        /// <param name="stream">Stream of incoming events data for one packet.</param>
        /// <returns>Number of metrics in packet.</returns>
        public int ReadMetricsCount(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                var version = reader.ReadUInt16();

                if (version >= 5)
                {
                    reader.ReadUInt32(); // for CRC
                }

                if (reader.ReadUInt32() != TypeSerializerFlags)
                {
                    throw new VersionNotSupportedMetricSerializationException("Type serializers not supported.");
                }

                stream.Position += 2 * sizeof(long); // skipping address of strings and address of metadata

                if (version >= 5)
                {
                    SerializationUtils.ReadInt64FromBase128(reader); // for packet time
                }

                return reader.ReadInt32();
            }
        }

        private void ReadMetricsData(
            BinaryReader reader,
            IFrontEndMetricBuilder<TMetadata> metricBuilder,
            ushort version,
            int maxMetricDimensionValueStringsLength)
        {
            long packetTime = 0;

            if (version >= 5)
            {
                packetTime = (long)SerializationUtils.ReadUInt64FromBase128(reader);
            }

            Stream readerStream = reader.BaseStream;

            var metricsCount = reader.ReadUInt32();
            for (var i = 0; i < metricsCount; ++i)
            {
                DateTime timeUtc;
                var count = 0U;
                var sum = default(MetricValueV2);
                var min = default(MetricValueV2);
                var max = default(MetricValueV2);
                double sumOfSquareDiffFromMean = 0;

                var metadata = this.ReadMetricMetadataByIndex(reader);

                if (version >= 5)
                {
                    var timeInTicks = (packetTime - SerializationUtils.ReadInt64FromBase128(reader)) * SerializationUtils.OneMinuteInterval;
                    timeUtc = new DateTime(timeInTicks, DateTimeKind.Utc);
                }
                else
                {
                    timeUtc = new DateTime((long)SerializationUtils.ReadUInt64FromBase128(reader), DateTimeKind.Utc);
                }

                for (var j = 0; j < metadata.DimensionsCount; ++j)
                {
                    var dimensionValue = this.ReadStringByIndex(reader);
                    if (dimensionValue.Length > maxMetricDimensionValueStringsLength)
                    {
                        throw new MetricSerializationException($"Dimension value string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricDimensionValueStringsLength}, Value:{dimensionValue}.", null, true);
                    }

                    this.reusableStringsList.Add(dimensionValue);
                }

                var samplingTypes = (SamplingTypes)SerializationUtils.ReadUInt32FromBase128(reader);
                var isDouble = (samplingTypes & SamplingTypes.DoubleValueType) != 0;
                var isDoubleStoredAslong = (samplingTypes & SamplingTypes.DoubleValueStoredAsLongType) != 0;

                if ((samplingTypes & SamplingTypes.Min) != 0)
                {
                    min = this.ReadMetricValue(reader, isDouble, isDoubleStoredAslong);
                }

                if ((samplingTypes & SamplingTypes.Max) != 0)
                {
                   max = this.ReadMetricValue(reader, isDouble, isDoubleStoredAslong);
                }

                if ((samplingTypes & SamplingTypes.Sum) != 0)
                {
                    sum = this.ReadMetricValue(reader, isDouble, isDoubleStoredAslong);
                }

                if ((samplingTypes & SamplingTypes.Count) != 0)
                {
                    count = SerializationUtils.ReadUInt32FromBase128(reader);
                }

                if ((samplingTypes & SamplingTypes.SumOfSquareDiffFromMean) != 0)
                {
                    sumOfSquareDiffFromMean = reader.ReadDouble();
                }

                bool haveHistogram = (samplingTypes & SamplingTypes.Histogram) != 0;
                metricBuilder.BeginMetricCreation(metadata, this.reusableStringsList, timeUtc, samplingTypes, count, sum, min, max, sumOfSquareDiffFromMean);
                this.reusableStringsList.Clear();

                if (haveHistogram)
                {
                    IEnumerable<KeyValuePair<ulong, uint>> histogramBuckets =
                        SerializationUtils.ReadHistogram(reader, hasHistogramSizePrefix: version > 3);

                    this.histogramBuffer.Clear();
                    this.histogramBuffer.AddRange(histogramBuckets);
                    metricBuilder.AssignHistogram(this.histogramBuffer);
                }

                if ((samplingTypes & SamplingTypes.HyperLogLogSketch) != 0)
                {
                    var sizeOfHyperLogLogSketches = reader.ReadInt32();
                    metricBuilder.AssignHyperLogLogSketch(reader, sizeOfHyperLogLogSketches);
                }

                if (version >= 6)
                {
                    bool haveTDigest = (samplingTypes & SamplingTypes.TDigest) != 0;
                    bool readTDigest = false;

                    // starting from version 6, there is a list of TLV-type
                    // (https://en.wikipedia.org/wiki/Type-length-value) tuples in the rest of the serialized metric.

                    // deserialize all of them ignoring the unknown ones.
                    // list TLV values is expected to contain a single end-of-list marker in the end with T = 0x00
                    uint type;
                    while ((type = SerializationUtils.ReadUInt32FromBase128(reader)) != 0x00)
                    {
                        int length = (int)SerializationUtils.ReadUInt32FromBase128(reader);
                        long nextPos = readerStream.Position + length;

                        switch (type)
                        {
                            case TDigestPrefixValue: // 0x74 == 't' (tDigest)
                                if (haveTDigest)
                                {
                                    if (!readTDigest)
                                    {
                                        metricBuilder.AssignTDigest(reader, length);
                                        readTDigest = true;
                                    }
                                    else
                                    {
                                        // if we already saw tDigest value and see it
                                        // second time it is a sign of a protocol error.
                                        throw new MetricSerializationException("Saw 2 TDigest values for the same metric", null, isInvalidData: true);
                                    }
                                }
                                else
                                {
                                    // if TLV list contains tDigest but the sampling types does not
                                    // it is a protocol error
                                    throw new MetricSerializationException("Sampling types do not contain tDigest, but TLV list contains it", null, isInvalidData: true);
                                }

                                break;

                            default:
                                // ignore unknown types
                                break;
                        }

                        // always set the position to point to the next entry.
                        // do not trust the deserializer code to leave the position set correctly
                        // this helps prevent compatibility problems and makes the deserializer
                        // more stable
                        readerStream.Position = nextPos;
                    }

                    if (haveTDigest && !readTDigest)
                    {
                        // if sampling types contain TDigest but we have not seen it in TLV this is
                        // a sign of protocol error
                        throw new MetricSerializationException("Sampling types contain tDigest, but TLV list does not contain it", null, isInvalidData: true);
                    }
                }

                metricBuilder.EndMetricCreation();
            }
        }

        private string ReadStringByIndex(BinaryReader reader)
        {
            var index = (int)SerializationUtils.ReadUInt32FromBase128(reader);
            return this.stringsDictionary[index];
        }

        private TMetadata ReadMetricMetadataByIndex(BinaryReader reader)
        {
            var index = (int)SerializationUtils.ReadUInt32FromBase128(reader);
            return this.metadataDictionary[index];
        }

        private TMetadata ReadMetricMetadata(BinaryReader reader, IFrontEndMetricBuilder<TMetadata> metricBuilder, int maxMetricStringsLength, int maxMetricNamespaceStringsLength)
        {
            var metricNamespace = this.ReadStringByIndex(reader);
            if (metricNamespace.Length > maxMetricNamespaceStringsLength)
            {
                throw new MetricSerializationException(
                    $"Namespace string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricNamespaceStringsLength}, Value:{metricNamespace}.", null, true);
            }

            var metricName = this.ReadStringByIndex(reader);
            if (metricName.Length > maxMetricStringsLength)
            {
                throw new MetricSerializationException(
                    $"Metric name string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricStringsLength}, Value:{metricName}.", null, true);
            }

            var count = SerializationUtils.ReadUInt32FromBase128(reader);
            for (var i = 0; i < count; ++i)
            {
                var dimensionName = this.ReadStringByIndex(reader);
                if (dimensionName.Length > maxMetricStringsLength)
                {
                    throw new MetricSerializationException(
                        $"Dimension name string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricStringsLength}, Value:{dimensionName}.", null, true);
                }

                this.reusableStringsList.Add(dimensionName);
            }

            var result = metricBuilder.CreateMetadata(metricNamespace, metricName, this.reusableStringsList);
            this.reusableStringsList.Clear();
            return result;
        }

        private MetricValueV2 ReadMetricValue(BinaryReader reader, bool isDouble, bool isDoubleStoredAsLong)
        {
            if (isDouble)
            {
                if (isDoubleStoredAsLong)
                {
                    return new MetricValueV2 { ValueAsDouble = SerializationUtils.ReadInt64FromBase128(reader) };
                }
                else
                {
                    return new MetricValueV2 { ValueAsDouble = reader.ReadDouble() };
                }
            }
            else
            {
                return new MetricValueV2 { ValueAsULong = SerializationUtils.ReadUInt64FromBase128(reader) };
            }
        }
    }
}
