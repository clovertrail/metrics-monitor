// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilteredTimeSeriesQueryResponse.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using Metrics;

    using Microsoft.Cloud.Metrics.Client.Configuration;

    using Newtonsoft.Json;
    using Online.Metrics.Serialization;
    using Online.Metrics.Serialization.Configuration;
    using Utility;

    /// <summary>
    /// TopN query response corresponding to <see cref="FilteredTimeSeriesQueryRequest" />.
    /// </summary>
    public sealed class FilteredTimeSeriesQueryResponse : IFilteredTimeSeriesQueryResponse
    {
        /// <summary>
        /// The version to indicate complete failure of a <see cref="FilteredTimeSeriesQueryResponse"/>.
        /// </summary>
        public const byte VersionToIndicateCompleteFailure = byte.MaxValue;

        /// <summary>
        /// The current stable version.
        /// </summary>
        public const byte CurrentVersion = 3;

        /// <summary>
        /// The next version to be supported in the future, if different from <see cref="CurrentVersion"/>.
        /// </summary>
        /// <remarks>
        /// Put version change history here.
        /// The initial version starts with 1.
        /// Version 2: Serializer and deserializer can process query messages in query result.
        /// Version 3: Handle more than 1 time series metadata as required in the support of multi-account query.
        /// </remarks>
        public const byte NextVersion = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredTimeSeriesQueryResponse"/> class.
        /// </summary>
        public FilteredTimeSeriesQueryResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredTimeSeriesQueryResponse" /> class.
        /// </summary>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <param name="timeResolutionInMinutes">Time resolution in mins</param>
        /// <param name="filteredTimeSeriesList">The time series returned</param>
        /// <param name="errorMessage">The error message.</param>
        internal FilteredTimeSeriesQueryResponse(
            DateTime startTime,
            DateTime endTime,
            int timeResolutionInMinutes,
            IReadOnlyList<FilteredTimeSeries> filteredTimeSeriesList,
            string errorMessage = null)
        {
            this.StartTimeUtc = startTime;
            this.EndTimeUtc = endTime;
            this.TimeResolutionInMinutes = timeResolutionInMinutes;
            this.FilteredTimeSeriesList = filteredTimeSeriesList;
            this.DiagnosticInfo = new DiagnosticInfo { ErrorMessage = errorMessage };
        }

        /// <summary>
        /// Gets the query request.
        /// </summary>
        public FilteredTimeSeriesQueryRequest QueryRequest { get; private set; }

        /// <summary>
        /// Gets the end time in UTC for the query results.
        /// </summary>
        public DateTime EndTimeUtc { get; private set; }

        /// <summary>
        /// Gets the start time in UTC for the query results.
        /// </summary>
        public DateTime StartTimeUtc { get; private set; }

        /// <summary>
        /// Gets the time resolution in milliseconds for the query results.
        /// </summary>
        public int TimeResolutionInMinutes { get; private set; }

        /// <summary>
        /// Gets the <see cref="FilteredTimeSeries"/> list. Each item represents a single time series where start time, end time and time resolution
        /// is represented by this object members.
        /// </summary>
        public IReadOnlyList<IFilteredTimeSeries> FilteredTimeSeriesList { get; private set; }

        /// <summary>
        /// Gets the error code.
        /// </summary>
        public FilteredTimeSeriesQueryResponseErrorCode ErrorCode { get; private set; } = FilteredTimeSeriesQueryResponseErrorCode.Success;

        /// <summary>
        /// Gets the diagnostics information.
        /// </summary>
        public DiagnosticInfo DiagnosticInfo { get; private set; }

        /// <summary>
        /// Deserializes to populate this object.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public void Deserialize(BinaryReader reader)
        {
            byte version;
            uint resultTimeSeriesCount;
            Dictionary<int, string> stringTable;
            long stringTableLengthInByte;
            SeriesMetadata seriesMetadata;
            Dictionary<int, SeriesMetadata> metadataTable;
            long metadataTableLengthInByte;

            if (!this.ReadPreamble(
                reader,
                out version,
                out resultTimeSeriesCount,
                out stringTable,
                out stringTableLengthInByte,
                out seriesMetadata,
                out metadataTable,
                out metadataTableLengthInByte))
            {
                return;
            }

            var filteredTimeSeriesList = new FilteredTimeSeries[resultTimeSeriesCount];
            for (int i = 0; i < resultTimeSeriesCount; i++)
            {
                filteredTimeSeriesList[i] = ReadTimeSeries(version, reader, seriesMetadata, stringTable, metadataTable);
            }

            this.FilteredTimeSeriesList = filteredTimeSeriesList;

            // we have read the string table and metadata tables, so just skip them to move to the correct position.
            reader.BaseStream.Position += metadataTableLengthInByte + stringTableLengthInByte;

            // additional query messages in version 2 and above
            // For now, we just read query messages. Further decisions need to be made on how to store messages
            if (version > 1)
            {
                int messageCount = reader.ReadByte();
                for (int i = 0; i < messageCount; i++)
                {
                    int topic = reader.ReadByte();
                    int level = reader.ReadByte();
                    int source = reader.ReadByte();
                    string content = reader.ReadString();
                }
            }
        }

        /// <summary>
        /// Deserializes to populate this object.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The IEnumerable with FilteredTimeSeries.</returns>
        public IEnumerable<FilteredTimeSeries> ReadFilteredTimeSeries(BinaryReader reader)
        {
            var numOfResponses = (int)SerializationUtils.ReadUInt32FromBase128(reader);
            for (int j = 0; j < numOfResponses; j++)
            {
                // Strip off a version added by query service host - QueryCoordinatorHost.cs.
                reader.ReadByte();

                var numOfQueryResults = reader.ReadInt32();
                for (int k = 0; k < numOfQueryResults; k++)
                {
                    byte version;
                    uint resultTimeSeriesCount;
                    Dictionary<int, string> stringTable;
                    long stringTableLengthInByte;
                    SeriesMetadata seriesMetadata;
                    Dictionary<int, SeriesMetadata> metadataTable;
                    long metadataTableLengthInByte;

                    if (!this.ReadPreamble(
                        reader,
                        out version,
                        out resultTimeSeriesCount,
                        out stringTable,
                        out stringTableLengthInByte,
                        out seriesMetadata,
                        out metadataTable,
                        out metadataTableLengthInByte))
                    {
                        yield break;
                    }

                    for (int i = 0; i < resultTimeSeriesCount; i++)
                    {
                        yield return ReadTimeSeries(version, reader, seriesMetadata, stringTable, metadataTable);
                    }
                }
            }
        }

        private static SeriesMetadata DeserializeTimeSeriesMetadata(BinaryReader reader, Dictionary<int, string> stringTable)
        {
            var metricIdentifier = new MetricIdentifier(
                DeserializeStringByIndex(reader, stringTable),
                DeserializeStringByIndex(reader, stringTable),
                DeserializeStringByIndex(reader, stringTable));

            var dimensionsCount = reader.ReadByte();
            var dimensionNames = new string[dimensionsCount];
            for (var i = 0; i < dimensionsCount; i++)
            {
                dimensionNames[i] = DeserializeStringByIndex(reader, stringTable);
            }

            return new SeriesMetadata(metricIdentifier, dimensionNames);
        }

        private static FilteredTimeSeries ReadTimeSeries(
            byte version,
            BinaryReader reader,
            SeriesMetadata seriesMetadata,
            Dictionary<int, string> stringTable,
            Dictionary<int, SeriesMetadata> metadataTable)
        {
            if (version >= 3)
            {
                var index = (int)SerializationUtils.ReadUInt32FromBase128(reader);
                seriesMetadata = metadataTable[index];
            }

            var metricIdentifier = seriesMetadata.MetricIdentifier;
            var dimensionNames = seriesMetadata.DimensionNames;

            var dimensionsCount = dimensionNames.Length;
            var dimensionList = new KeyValuePair<string, string>[dimensionsCount];
            for (int k = 0; k < dimensionsCount; k++)
            {
                dimensionList[k] = new KeyValuePair<string, string>(dimensionNames[k], DeserializeStringByIndex(reader, stringTable));
            }

            var propertiesCount = reader.ReadByte();
            var evaluatedValues = new KeyValuePair<string, double>[propertiesCount];
            for (int k = 0; k < propertiesCount; k++)
            {
                evaluatedValues[k] = new KeyValuePair<string, double>(DeserializeStringByIndex(reader, stringTable), reader.ReadDouble());
            }

            var samplingTypesCount = reader.ReadByte();
            var timeSeriesData = new KeyValuePair<SamplingType, double[]>[samplingTypesCount];
            for (int k = 0; k < samplingTypesCount; k++)
            {
                var samplingTypeString = DeserializeStringByIndex(reader, stringTable);
                var samplingType = SamplingType.BuiltInSamplingTypes.ContainsKey(samplingTypeString)
                    ? SamplingType.BuiltInSamplingTypes[samplingTypeString]
                    : new SamplingType(samplingTypeString);
                timeSeriesData[k] = new KeyValuePair<SamplingType, double[]>(samplingType, DoubleValueSerializer.Deserialize(reader));
            }

            double evaluatedResult = propertiesCount == 0 ? double.NaN : evaluatedValues[0].Value;
            return new FilteredTimeSeries(metricIdentifier, dimensionList, evaluatedResult, timeSeriesData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string DeserializeStringByIndex(BinaryReader reader, Dictionary<int, string> stringTable)
        {
            var index = (int)SerializationUtils.ReadUInt32FromBase128(reader);
            return stringTable[index];
        }

        private bool ReadPreamble(
            BinaryReader reader,
            out byte version,
            out uint resultTimeSeriesCount,
            out Dictionary<int, string> stringTable,
            out long stringTableLengthInByte,
            out SeriesMetadata seriesMetadata,
            out Dictionary<int, SeriesMetadata> metadataTable,
            out long metadataTableLengthInByte)
        {
            resultTimeSeriesCount = 0;
            stringTable = null;
            stringTableLengthInByte = 0;
            seriesMetadata = null;
            metadataTable = null;
            metadataTableLengthInByte = 0;

            version = reader.ReadByte();

            if (version == 0)
            {
                throw new MetricsClientException(
                    $"The server didn't respond with the right version of serialization - the initial version is 1 but the server responds with version 0.");
            }

            this.DiagnosticInfo = new DiagnosticInfo();
            if (version == VersionToIndicateCompleteFailure)
            {
                this.ErrorCode = (FilteredTimeSeriesQueryResponseErrorCode)reader.ReadInt16();

                // The trace ID and others will be filled in by callers.
                this.DiagnosticInfo.ErrorMessage = reader.ReadString();

                bool returnRequestObjectOnFailure = reader.ReadBoolean();
                if (returnRequestObjectOnFailure)
                {
                    this.QueryRequest = JsonConvert.DeserializeObject<FilteredTimeSeriesQueryRequest>(reader.ReadString());
                }

                return false;
            }

            if (version > NextVersion)
            {
                throw new MetricsClientException(
                    $"The server didn't respond with the right version of serialization. CurrentVersion : {CurrentVersion}, NextVersion : {NextVersion}, Responded: {version}.");
            }

            var hasDataQualityInfo = reader.ReadBoolean();
            if (hasDataQualityInfo)
            {
                var queryResultQualityInfo = new QueryResultQualityInfo();
                queryResultQualityInfo.Deserialize(reader);
            }

            this.StartTimeUtc = new DateTime(
                (long)SerializationUtils.ReadUInt64FromBase128(reader) * TimeSpan.TicksPerMinute,
                DateTimeKind.Utc);
            this.EndTimeUtc = this.StartTimeUtc.AddMinutes(SerializationUtils.ReadUInt32FromBase128(reader));
            this.TimeResolutionInMinutes = (int)SerializationUtils.ReadUInt32FromBase128(reader);
            resultTimeSeriesCount = SerializationUtils.ReadUInt32FromBase128(reader);

            /* Read string table. */
            var currentPosition = reader.BaseStream.Position;
            var stringTableRelativePosition = reader.ReadUInt64();
            reader.BaseStream.Position = currentPosition + (long)stringTableRelativePosition;

            var stringTableStartPostion = reader.BaseStream.Position;
            var stringTableSize = SerializationUtils.ReadUInt32FromBase128(reader);
            stringTable = new Dictionary<int, string>((int)stringTableSize);
            for (int i = 0; i < stringTableSize; i++)
            {
                stringTable.Add(i, reader.ReadString());
            }

            stringTableLengthInByte = reader.BaseStream.Position - stringTableStartPostion;

            // + sizeof (double) since we just read the stringTableRelativePosition as double.
            reader.BaseStream.Position = currentPosition + sizeof(double);

            if (version >= 3)
            {
                /* Read metadata table */
                currentPosition = reader.BaseStream.Position;
                var metadataTableRelativePosition = reader.ReadUInt64();
                reader.BaseStream.Position = currentPosition + (long)metadataTableRelativePosition;

                var metadataTableStartPostion = reader.BaseStream.Position;
                var metadataTableSize = SerializationUtils.ReadUInt32FromBase128(reader);

                metadataTable = new Dictionary<int, SeriesMetadata>((int)metadataTableSize);
                for (var i = 0; i < metadataTableSize; i++)
                {
                    metadataTable.Add(i, DeserializeTimeSeriesMetadata(reader, stringTable));
                }

                metadataTableLengthInByte = reader.BaseStream.Position - metadataTableStartPostion;

                // + sizeof (double) since we just read the metadataTableRelativePosition as double.
                reader.BaseStream.Position = currentPosition + sizeof(double);
            }

            if (resultTimeSeriesCount > 0)
            {
                if (version < 3)
                {
                    seriesMetadata = DeserializeTimeSeriesMetadata(reader, stringTable);
                    this.QueryRequest = new FilteredTimeSeriesQueryRequest(seriesMetadata.MetricIdentifier);
                }
                else
                {
                    this.QueryRequest = new FilteredTimeSeriesQueryRequest(metadataTable.Values.First().MetricIdentifier);
                }
            }

            return true;
        }

        private sealed class SeriesMetadata
        {
            public SeriesMetadata(MetricIdentifier metricIdentifier, string[] dimensionNames)
            {
                this.MetricIdentifier = metricIdentifier;
                this.DimensionNames = dimensionNames;
            }

            public MetricIdentifier MetricIdentifier { get; }

            public string[] DimensionNames { get; }
        }
    }
}
