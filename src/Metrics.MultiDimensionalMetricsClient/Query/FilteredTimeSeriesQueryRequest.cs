// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilteredTimeSeriesQueryRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System;
    using System.Collections.Generic;
    using Metrics;
    using Newtonsoft.Json;
    using Online.Metrics.Serialization.Configuration;

    /// <summary>
    /// Request to query data with dimensional filters and at Top N condition and retrieve the
    /// resultant series.
    /// </summary>
    public sealed class FilteredTimeSeriesQueryRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredTimeSeriesQueryRequest" /> class.
        /// </summary>
        /// <param name="metricIdentifier">The metric identifier.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="seriesResolutionInMinutes">The series resolution in minutes.</param>
        /// <param name="aggregationType">Type of the aggregation.</param>
        /// <param name="topPropertyDefinition">The top property definition.</param>
        /// <param name="numberOfResultsToReturn">The number of results to return.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="zeroAsNoValueSentinel">Indicates whether zero should be used as no value sentinel, or double.NaN.</param>
        /// <param name="outputDimensionNames">The dimension names to be used for the result time series.  If not set, same as the dimensions in the dimension filter.</param>
        /// <param name="lastValueMode">Indicating if the query should be fulfilled with last value mode. If true, null values in the query range requested will be filled with the last known value.</param>
        public FilteredTimeSeriesQueryRequest(
            MetricIdentifier metricIdentifier,
            IReadOnlyList<SamplingType> samplingTypes,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            int seriesResolutionInMinutes,
            AggregationType aggregationType,
            PropertyDefinition topPropertyDefinition,
            int numberOfResultsToReturn,
            OrderBy orderBy,
            bool zeroAsNoValueSentinel,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false)
            : this(
                metricIdentifier,
                null,
                null,
                null,
                samplingTypes,
                dimensionFilters,
                startTimeUtc,
                endTimeUtc,
                seriesResolutionInMinutes,
                aggregationType,
                topPropertyDefinition,
                numberOfResultsToReturn,
                orderBy,
                zeroAsNoValueSentinel,
                false,
                outputDimensionNames,
                lastValueMode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredTimeSeriesQueryRequest" /> class.
        /// </summary>
        /// <param name="monitoringAccountNames">The monitoring account names.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="seriesResolutionInMinutes">The series resolution in minutes.</param>
        /// <param name="aggregationType">Type of the aggregation.</param>
        /// <param name="topPropertyDefinition">The top property definition.</param>
        /// <param name="numberOfResultsToReturn">The number of results to return.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="zeroAsNoValueSentinel">Indicates whether zero should be used as no value sentinel, or double.NaN.</param>
        /// <param name="aggregateAcrossAccounts">if set to <c>true</c>, aggregate data across accounts, and the account name in the query results is "AccountMoniker".</param>
        /// <param name="outputDimensionNames">The dimension names to be used for the result time series.  If not set, same as the dimensions in the dimension filter.</param>
        /// <param name="lastValueMode">Indicating if the query should be fulfilled with last value mode.</param>
        public FilteredTimeSeriesQueryRequest(
            IReadOnlyList<string> monitoringAccountNames,
            string metricNamespace,
            string metricName,
            IReadOnlyList<SamplingType> samplingTypes,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            int seriesResolutionInMinutes,
            AggregationType aggregationType,
            PropertyDefinition topPropertyDefinition,
            int numberOfResultsToReturn,
            OrderBy orderBy,
            bool zeroAsNoValueSentinel,
            bool aggregateAcrossAccounts,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false)
            : this(
                default(MetricIdentifier),
                monitoringAccountNames,
                metricNamespace,
                metricName,
                samplingTypes,
                dimensionFilters,
                startTimeUtc,
                endTimeUtc,
                seriesResolutionInMinutes,
                aggregationType,
                topPropertyDefinition,
                numberOfResultsToReturn,
                orderBy,
                zeroAsNoValueSentinel,
                aggregateAcrossAccounts,
                outputDimensionNames,
                lastValueMode)
        {
            if (monitoringAccountNames == null || monitoringAccountNames.Count == 0)
            {
                throw new ArgumentException("must not be null or empty", nameof(monitoringAccountNames));
            }

            for (int i = 0; i < monitoringAccountNames.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(monitoringAccountNames[i]))
                {
                    throw new ArgumentException($"All monitoring accounts must not be null or empty: {string.Join(",", monitoringAccountNames)}.", nameof(monitoringAccountNames));
                }
            }

            // Make de-serialization not throw: MetricIdentifier is a struct and in addition it doesn't allow null-or-empty property members.
            this.MetricIdentifier = new MetricIdentifier(monitoringAccountNames[0], metricNamespace, metricName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredTimeSeriesQueryRequest"/> class.
        /// </summary>
        /// <param name="metricIdentifier">The metric identifier.</param>
        /// <remarks>For OBO, we fill in only the <see cref="MetricIdentifier"/> property on success but we do fill in the full request on failure.</remarks>
        internal FilteredTimeSeriesQueryRequest(MetricIdentifier metricIdentifier)
        {
            this.MetricIdentifier = metricIdentifier;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredTimeSeriesQueryRequest" /> class.
        /// </summary>
        /// <param name="metricIdentifier">The metric identifier.</param>
        /// <param name="monitoringAccountNames">The monitoring account names.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="seriesResolutionInMinutes">The series resolution in minutes.</param>
        /// <param name="aggregationType">Type of the aggregation.</param>
        /// <param name="topPropertyDefinition">The top property definition.</param>
        /// <param name="numberOfResultsToReturn">The number of results to return.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="zeroAsNoValueSentinel">Indicates whether zero should be used as no value sentinel, or double.NaN.</param>
        /// <param name="aggregateAcrossAccounts">if set to <c>true</c>, aggregate data across accounts, and the account name in the query results is "AccountMoniker".</param>
        /// <param name="outputDimensionNames">The dimension names to be used for the result time series.  If not set, same as the dimensions in the dimension filter.</param>
        /// <param name="lastValueMode">Indicating if the query should be fulfilled with last value mode. If true, null values in the query range requested will be filled with the last known value.</param>
        [JsonConstructor]
        private FilteredTimeSeriesQueryRequest(
            MetricIdentifier metricIdentifier,
            IReadOnlyList<string> monitoringAccountNames,
            string metricNamespace,
            string metricName,
            IReadOnlyList<SamplingType> samplingTypes,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            int seriesResolutionInMinutes,
            AggregationType aggregationType,
            PropertyDefinition topPropertyDefinition,
            int numberOfResultsToReturn,
            OrderBy orderBy,
            bool zeroAsNoValueSentinel,
            bool aggregateAcrossAccounts,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false)
        {
            this.MetricIdentifier = metricIdentifier;
            this.MonitoringAccountNames = monitoringAccountNames;
            this.MetricNamespace = metricNamespace;
            this.MetricName = metricName;
            this.SamplingTypes = samplingTypes;
            this.DimensionFilters = dimensionFilters;
            this.StartTimeUtc = startTimeUtc;
            this.EndTimeUtc = endTimeUtc;
            this.SeriesResolutionInMinutes = seriesResolutionInMinutes;
            this.AggregationType = aggregationType;
            this.TopPropertyDefinition = topPropertyDefinition;
            this.NumberOfResultsToReturn = numberOfResultsToReturn;
            this.OrderBy = orderBy;
            this.ZeroAsNoValueSentinel = zeroAsNoValueSentinel;
            this.AggregateAcrossAccounts = aggregateAcrossAccounts;
            this.OutputDimensionNames = outputDimensionNames;
            this.LastValueMode = lastValueMode;
        }

        /// <summary>
        /// Gets the monitoring accounts to be queried.
        /// </summary>
        public IReadOnlyList<string> MonitoringAccountNames { get; }

        /// <summary>
        /// Gets the metric namespace.
        /// </summary>
        public string MetricNamespace { get; }

        /// <summary>
        /// Gets the name of the metric.
        /// </summary>
        public string MetricName { get; }

        /// <summary>
        /// Gets the metric identifier to be queried.
        /// </summary>
        public MetricIdentifier MetricIdentifier { get; }

        /// <summary>
        /// Gets the sampling types to be queried.
        /// </summary>
        public IReadOnlyList<SamplingType> SamplingTypes { get; }

        /// <summary>
        /// Gets the dimension filters used to determine which time series keys should be
        /// retrieved from the data store.
        /// </summary>
        public IReadOnlyList<DimensionFilter> DimensionFilters { get; }

        /// <summary>
        /// Gets the start time of the query period in UTC.
        /// </summary>
        public DateTime StartTimeUtc { get; }

        /// <summary>
        /// Gets the end time of the query period in UTC.
        /// </summary>
        public DateTime EndTimeUtc { get; }

        /// <summary>
        /// Gets the series resolution in minutes.  One data point will represent x number
        /// of minutes of raw data.
        /// </summary>
        public int SeriesResolutionInMinutes { get; }

        /// <summary>
        /// Gets the type of the aggregation to be performed when resolution is being reduced.
        /// </summary>
        public AggregationType AggregationType { get; }

        /// <summary>
        /// Gets the top property definition.  This defines which sampling type
        /// </summary>
        public PropertyDefinition TopPropertyDefinition { get; }

        /// <summary>
        /// Gets the number of results to return.
        /// </summary>
        public int NumberOfResultsToReturn { get; }

        /// <summary>
        /// Gets the ordering of the results, either Ascending or Descending.
        /// </summary>
        public OrderBy OrderBy { get; }

        /// <summary>
        /// Indicates if zero or double.NaN should be used to indicate no value in time series data.
        /// </summary>
        public bool ZeroAsNoValueSentinel { get; }

        /// <summary>
        /// Gets a value indicationg whether to aggregate data across accounts.
        /// </summary>
        public bool AggregateAcrossAccounts { get; }

        /// <summary>
        /// Gets or sets the output dimension names to be returned with the result time series.  If the output dimension names are not specified,
        /// The dimensions used as dimension filters will be the output dimensions.
        /// </summary>
        public IReadOnlyList<string> OutputDimensionNames { get; }

        /// <summary>
        /// Indicates if the query should be fulfilled with last value mode. If true, null values in the query range requested will be filled with the last known value.
        /// </summary>
        public bool LastValueMode { get; }
    }
}
