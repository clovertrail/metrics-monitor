// -----------------------------------------------------------------------
// <copyright file="MetricDeserializer.cs" company="Microsoft Corporation">
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
    public sealed class MetricDeserializer<TMetadata>
        where TMetadata : IMetricMetadata
    {
        private const ushort MaxVersion = 5;
        private const uint TypeSerializerFlags = 0x12020000; // Corresponds to 0001.001.0000.0001.00000000000000000 (Use string and metadata interning with variable-length integer serialization)
        private readonly List<string> stringsDictionary = new List<string>();
        private readonly List<TMetadata> metadataDictionary = new List<TMetadata>();
        private readonly List<KeyValuePair<ulong, uint>> histogramBuffer = new List<KeyValuePair<ulong, uint>>();

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
        /// Clears the deserializer state.
        /// </summary>
        public void Clear()
        {
            this.stringsDictionary.Clear();
            this.metadataDictionary.Clear();
            this.histogramBuffer.Clear();
        }

        /// <summary>
        /// Deserializes counter (metric) data from the stream and adds all objects to provided collection.
        /// </summary>
        /// <param name="stream">Stream from which data should be deserialized. Stream should be readable and provide randon access.</param>
        /// <param name="metricBuilder">An object responsible for creation and further consumption of deserialized data.</param>
        /// <param name="maxMetricStringsLength">Maximum length of strings, which represent metric name, dimension names and values.</param>
        /// <param name="maxMetricNamespaceStringsLength">Maximum length of metric namespace string.</param>
        /// <param name="maxMetricDimensionValueStringsLength">Maximum length of metric dimension value string.</param>
        public void Deserialize(Stream stream, IMetricBuilder<TMetadata> metricBuilder, int maxMetricStringsLength, int maxMetricNamespaceStringsLength, int maxMetricDimensionValueStringsLength)
        {
            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException(@"Stream should be readable and provide random access.", nameof(stream));
            }

            try
            {
                using (var reader = new NoCloseBinaryReader(stream, Encoding.UTF8))
                {
                    var startStreamPosition = stream.Position;

                    // Read version and type serializers info
                    var version = reader.ReadUInt16();
                    if (version > MaxVersion)
                    {
                        throw new VersionNotSupportedMetricSerializationException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Version is not supported. Read version:{0}, Max version:{1}.",
                                version,
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
                        this.stringsDictionary.Add(reader.ReadString());
                    }

                    // Read metrics metadata
                    stream.Position = deserializerDataPosition;
                    stream.Position = startStreamPosition + reader.ReadInt64();
                    count = SerializationUtils.ReadUInt32FromBase128(reader);
                    for (uint i = 0; i < count; ++i)
                    {
                        this.metadataDictionary.Add(this.ReadMetricMetadata(reader, metricBuilder, version, maxMetricStringsLength, maxMetricNamespaceStringsLength));
                    }

                    // Read metrics data
                    stream.Position = deserializerDataPosition + (2 * sizeof(long));
                    this.ReadMetricsData(reader, metricBuilder, version, maxMetricStringsLength, maxMetricNamespaceStringsLength, maxMetricDimensionValueStringsLength);
                }
            }
            catch (IOException ioException)
            {
                throw new MetricSerializationException("Failed to deserialize data. Problem with input stream.", ioException);
            }
            catch (Exception exception)
            {
                throw new MetricSerializationException("Failed to deserialize data. Likely the incoming stream contains corrupted data.", exception);
            }
            finally
            {
                this.metadataDictionary.Clear();
                this.stringsDictionary.Clear();
                this.histogramBuffer.Clear();
            }
        }

        private List<KeyValuePair<ulong, uint>> ReadHistogram(BinaryReader reader, ushort version)
        {
            SerializationUtils.ReadHistogramTo(this.histogramBuffer, reader, version > 3);

            return this.histogramBuffer;
        }

        private void ReadMetricsData(BinaryReader reader, IMetricBuilder<TMetadata> metricBuilder, ushort version, int maxMetricStringsLength, int maxMetricNamespaceStringsLength, int maxMetricDimensionValueStringsLength)
        {
            long packetTime = 0;

            if (version >= 5)
            {
                packetTime = (long)SerializationUtils.ReadUInt64FromBase128(reader);
            }

            // Versions before 2 used variable number of bytes to write number of serialized metrics data.
            // From version 2 passing IEnumerable<IReadOnlyMetric> is supported, thus number of metrics data
            // is unknown beforehand and we cannot use variable number anymore. Thus fixed 4 bytes uint is used.
            var count = version >= 2 ? reader.ReadUInt32() : SerializationUtils.ReadUInt32FromBase128(reader);
            for (var i = 0; i < count; ++i)
            {
                metricBuilder.BeginMetricCreation();
                var metadata = this.ReadMetricMetadataByIndex(reader);
                metricBuilder.AssignMetadata(metadata);

                // In versions 0-2 Monitoring Account and Metric Namespace was part of the Metric data
                // From version 3 Monitoring Account is removed and Metric Namespace became a part of Metric Metadata
                if (version < 3)
                {
                    var monitoringAccount = this.ReadStringByIndex(reader);
                    if (monitoringAccount.Length > maxMetricStringsLength)
                    {
                        throw new MetricSerializationException($"Monitoring account string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricStringsLength}, Value:{monitoringAccount}.", null);
                    }

                    var metricNamespace = this.ReadStringByIndex(reader);
                    if (metricNamespace.Length > maxMetricNamespaceStringsLength)
                    {
                        throw new MetricSerializationException($"Namespace string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricNamespaceStringsLength}, Value:{metricNamespace}.", null);
                    }

                    metricBuilder.AssignMonitoringAccount(monitoringAccount);
                    metricBuilder.AssignNamespace(metricNamespace);
                }

                if (version == 0)
                {
                    // Skip event id
                    this.ReadStringByIndex(reader);
                }

                if (version >= 5)
                {
                    var timeInTicks = (packetTime - SerializationUtils.ReadInt64FromBase128(reader)) * SerializationUtils.OneMinuteInterval;
                    metricBuilder.AssignTimeUtc(new DateTime(timeInTicks, DateTimeKind.Utc));
                }
                else
                {
                    metricBuilder.AssignTimeUtc(new DateTime((long)SerializationUtils.ReadUInt64FromBase128(reader), DateTimeKind.Utc));
                }

                for (var j = 0; j < metadata.DimensionsCount; ++j)
                {
                    var dimensionValue = this.ReadStringByIndex(reader);
                    if (dimensionValue.Length > maxMetricDimensionValueStringsLength)
                    {
                        throw new MetricSerializationException($"Dimension value string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricDimensionValueStringsLength}, Value:{dimensionValue}.", null);
                    }

                    metricBuilder.AddDimensionValue(dimensionValue);
                }

                var samplingTypes = (SamplingTypes)SerializationUtils.ReadUInt32FromBase128(reader);
                metricBuilder.AssignSamplingTypes(samplingTypes);

                if ((samplingTypes & SamplingTypes.Min) != 0)
                {
                    metricBuilder.AssignMin(SerializationUtils.ReadUInt64FromBase128(reader));
                }

                if ((samplingTypes & SamplingTypes.Max) != 0)
                {
                    metricBuilder.AssignMax(SerializationUtils.ReadUInt64FromBase128(reader));
                }

                if ((samplingTypes & SamplingTypes.Sum) != 0)
                {
                    metricBuilder.AssignSum(SerializationUtils.ReadUInt64FromBase128(reader));
                }

                if ((samplingTypes & SamplingTypes.Count) != 0)
                {
                    metricBuilder.AssignCount(SerializationUtils.ReadUInt32FromBase128(reader));
                }

                if ((samplingTypes & SamplingTypes.SumOfSquareDiffFromMean) != 0)
                {
                    var sumOfSquareDiffFromMean = reader.ReadDouble();
                    metricBuilder.AssignSumOfSquareDiffFromMean(sumOfSquareDiffFromMean);
                }

                if ((samplingTypes & SamplingTypes.Histogram) != 0)
                {
                    metricBuilder.AssignHistogram(this.ReadHistogram(reader, version));
                }

                if ((samplingTypes & SamplingTypes.HyperLogLogSketch) != 0)
                {
                    var sizeOfHyperLogLogSketches = reader.ReadInt32();
                    metricBuilder.AssignHyperLogLogSketch(reader, sizeOfHyperLogLogSketches);
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

        private TMetadata ReadMetricMetadata(BinaryReader reader, IMetricBuilder<TMetadata> metricBuilder, ushort version, int maxMetricStringsLength, int maxMetricNamespaceStringsLength)
        {
            var metricNamespace = string.Empty;

            // In versions 0-2 Metric Namespace was part of the Metric data, from version 3 it became a part of Metric Metadata
            if (version >= 3)
            {
                metricNamespace = this.ReadStringByIndex(reader);
                if (metricNamespace.Length > maxMetricNamespaceStringsLength)
                {
                    throw new MetricSerializationException($"Namespace string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricNamespaceStringsLength}, Value:{metricNamespace}.", null);
                }
            }

            var metricName = this.ReadStringByIndex(reader);
            if (metricName.Length > maxMetricStringsLength)
            {
                throw new MetricSerializationException($"Metric name string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricStringsLength}, Value:{metricName}.", null);
            }

            var count = SerializationUtils.ReadUInt32FromBase128(reader);
            var dimensionNames = new List<string>((int)count);
            for (var i = 0; i < count; ++i)
            {
                var dimensionName = this.ReadStringByIndex(reader);
                if (dimensionName.Length > maxMetricStringsLength)
                {
                    throw new MetricSerializationException($"Dimension name string in the packet exceeds preconfigured length. Packet is corrupted. MaxLength:{maxMetricStringsLength}, Value:{dimensionName}.", null);
                }

                dimensionNames.Add(dimensionName);
            }

            return metricBuilder.CreateMetadata(metricNamespace, metricName, dimensionNames);
        }

        /// <summary>
        /// No close binary reader.
        /// </summary>
        public class NoCloseBinaryReader : BinaryReader
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NoCloseBinaryReader"/> class.
            /// </summary>
            /// <param name="stream">The stream.</param>
            /// <param name="encoding">The encoding.</param>
            public NoCloseBinaryReader(Stream stream, Encoding encoding)
                : base(stream, encoding)
            {
            }

            /// <inheritdoc />
            protected override void Dispose(bool disposing)
            {
                this.BaseStream.Flush();
                base.Dispose(false);
            }
        }
    }
}
