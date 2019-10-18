// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMetricReader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Cloud.Metrics.Client.Query;
    using Microsoft.Online.Metrics.Serialization.Configuration;

    /// <summary>
    /// The interface for reading metric data.
    /// </summary>
    public interface IMetricReader
    {
        /// <summary>
        /// Gets the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="definition">The time series definition.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definition"/>.
        /// </returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<TimeSeries<MetricIdentifier, double?>> GetTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            TimeSeriesDefinition<MetricIdentifier> definition);

        /// <summary>
        /// Gets the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="definition">The time series definition.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definition"/>.
        /// </returns>
        /// <remarks>This API uses <see cref="AggregationType.Automatic"/> by default and other overloads are available for specific <see cref="AggregationType"/>.</remarks>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<TimeSeries<MetricIdentifier, double?>> GetTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            int seriesResolutionInMinutes,
            TimeSeriesDefinition<MetricIdentifier> definition);

        /// <summary>
        /// Gets time series with multiple sampling types.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="definition">The time series definition.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="aggregationType">The aggregation function used to reduce the resolution of the returned series.</param>
        /// <returns>
        /// The time series for the given <paramref name="definition" />.
        /// </returns>
        Task<TimeSeries<MetricIdentifier, double?>> GetTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType[] samplingTypes,
            TimeSeriesDefinition<MetricIdentifier> definition,
            int seriesResolutionInMinutes = 1,
            AggregationType aggregationType = AggregationType.Automatic);

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions"/>.
        /// </returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            params TimeSeriesDefinition<MetricIdentifier>[] definitions);

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions"/>.
        /// </returns>
        /// <remarks>This API uses <see cref="AggregationType.Automatic"/> by default and other overloads are available for specific <see cref="AggregationType"/>.</remarks>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            int seriesResolutionInMinutes,
            params TimeSeriesDefinition<MetricIdentifier>[] definitions);

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions"/>.
        /// </returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            IEnumerable<TimeSeriesDefinition<MetricIdentifier>> definitions);

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <remarks>This API uses <see cref="AggregationType.Automatic"/> by default and other overloads are available for specific <see cref="AggregationType"/>.</remarks>
        /// <returns>
        /// The time series of for the given <paramref name="definitions"/>.
        /// </returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            int seriesResolutionInMinutes,
            IEnumerable<TimeSeriesDefinition<MetricIdentifier>> definitions);

        /// <summary>
        /// Gets a list of the time series, each with multiple sampling types.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="aggregationType">The aggregation function used to reduce the resolution of the returned series.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions" />.
        /// </returns>
        Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType[] samplingTypes,
            IEnumerable<TimeSeriesDefinition<MetricIdentifier>> definitions,
            int seriesResolutionInMinutes = 1,
            AggregationType aggregationType = AggregationType.Automatic);

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions"/>.
        /// </returns>
        Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(IList<TimeSeriesDefinition<MetricIdentifier>> definitions);

        /// <summary>
        /// Gets the list of namespaces for the <paramref name="monitoringAccount"/>.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <returns>The list of namespaces for the <paramref name="monitoringAccount"/>.</returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<IReadOnlyList<string>> GetNamespacesAsync(string monitoringAccount);

        /// <summary>
        /// Gets the list of metric names for the <paramref name="monitoringAccount" /> and <paramref name="metricNamespace" />.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>
        /// The list of metric names for the <paramref name="monitoringAccount" /> and <paramref name="metricNamespace" />.
        /// </returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<IReadOnlyList<string>> GetMetricNamesAsync(string monitoringAccount, string metricNamespace);

        /// <summary>
        /// Gets the list of dimension names for the <paramref name="metricId" />.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <returns>
        /// The list of dimension names for the <paramref name="metricId" />.
        /// </returns>
        Task<IReadOnlyList<string>> GetDimensionNamesAsync(MetricIdentifier metricId);

        /// <summary>
        /// Gets the list of pre-aggregate configurations for the <paramref name="metricId" />.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <returns>
        /// The list of pre-aggregate configurations for the <paramref name="metricId" />.
        /// </returns>
        Task<IReadOnlyList<PreAggregateConfiguration>> GetPreAggregateConfigurationsAsync(MetricIdentifier metricId);

        /// <summary>
        /// Gets the known dimension combinations.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <returns>Time series definitions with  known dimension combinations.</returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<IReadOnlyList<TimeSeriesDefinition<MetricIdentifier>>> GetKnownTimeSeriesDefinitionsAsync(
            MetricIdentifier metricId,
            params DimensionFilter[] dimensionFilters);

        /// <summary>
        /// Gets the known dimension combinations.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <returns>Time series definitions with  known dimension combinations.</returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<IReadOnlyList<TimeSeriesDefinition<MetricIdentifier>>> GetKnownTimeSeriesDefinitionsAsync(
            MetricIdentifier metricId,
            IEnumerable<DimensionFilter> dimensionFilters);

        /// <summary>
        /// Gets the dimension values for <paramref name="dimensionName"/> satifying the <paramref name="dimensionFilters"/>
        /// and time range (<paramref name="startTimeUtc"/>, <paramref name="endTimeUtc"/>)
        /// </summary>
        /// <remarks>
        /// Time range resolution is day.
        /// </remarks>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">
        /// The dimension filters representing the pre-aggregate dimensions.
        /// Create an emtpy include filter for dimension with no filter values.
        /// Requested dimension should also be part of this and should be empty.
        /// </param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for <paramref name="dimensionName"/>.</returns>
        Task<IReadOnlyList<string>> GetDimensionValuesAsync(
            MetricIdentifier metricId,
            List<DimensionFilter> dimensionFilters,
            string dimensionName,
            DateTime startTimeUtc,
            DateTime endTimeUtc);

        /// <summary>
        /// Gets the known dimension combinations.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <param name="startTimeUtc">Start time for dimension values.</param>
        /// <param name="endTimeUtc">End time for dimension values.</param>
        /// <param name="newCombinationsOnly">
        /// If true only combinations which were added into memory in this session of hinting system after fromTimeUtc.
        /// This flag does *not* guarantee that only new combinations will be returned
        /// It is more of a hint to the hinting system to try to give only new combinations in given time range.
        /// </param>
        /// <returns>Time series definitions with  known dimension combinations.</returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        Task<IReadOnlyList<TimeSeriesDefinition<MetricIdentifier>>> GetKnownTimeSeriesDefinitionsAsync(
            MetricIdentifier metricId,
            IEnumerable<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            bool newCombinationsOnly = false);

        /// <summary>
        /// Gets the filtered dimension values asynchronously.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="reducer">The reducing function to apply to the time series.</param>
        /// <param name="queryFilter">The query filter.</param>
        /// <param name="includeSeries">if set to <c>true</c> include series values.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.</param>
        /// <returns>A list of filtered dimension values.</returns>
        [Obsolete]
        Task<IReadOnlyList<IQueryResult>> GetFilteredDimensionValuesAsync(
            MetricIdentifier metricId,
            IEnumerable<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            Reducer reducer,
            QueryFilter queryFilter,
            bool includeSeries,
            SelectionClause selectionClause = null,
            AggregationType aggregationType = AggregationType.Sum,
            long seriesResolutionInMinutes = 1);

        /// <summary>
        /// Gets the filtered dimension values asynchronously.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="reducer">The reducing function to apply to the time series.</param>
        /// <param name="queryFilter">The query filter.</param>
        /// <param name="includeSeries">if set to <c>true</c> include series values.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.</param>
        /// <returns>A list of filtered dimension values.</returns>
        [Obsolete]
        Task<QueryResultsList> GetFilteredDimensionValuesAsyncV2(
            MetricIdentifier metricId,
            IEnumerable<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            Reducer reducer,
            QueryFilter queryFilter,
            bool includeSeries,
            SelectionClause selectionClause = null,
            AggregationType aggregationType = AggregationType.Sum,
            long seriesResolutionInMinutes = 1);

        /// <summary>
        /// Gets the time series values that match the filtering criteria.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.  By default, all results are returned.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.  By default, automatic resolution is used (same as Jarvis UI).</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.  1 minute resolution (full resolution in MDM today) by default.</param>
        /// <param name="traceId">The trace identifier for the query, used for diagnostic purposes only.  If a trace id is not provided, one will be generated.</param>
        /// <param name="outputDimensionNames">The output dimension names.</param>
        /// <param name="lastValueMode">Indicating if the query should be fulfilled with last value mode. If true, null values in the query range requested will be filled with the last known value.</param>
        /// <returns>
        /// Time series definitions matching the query criteria.
        /// </returns>
        [Obsolete]
        Task<IQueryResultListV3> GetFilteredDimensionValuesAsyncV3(
            MetricIdentifier metricId,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            IReadOnlyList<SamplingType> samplingTypes,
            SelectionClauseV3 selectionClause = null,
            AggregationType aggregationType = AggregationType.Automatic,
            long seriesResolutionInMinutes = 1,
            Guid? traceId = null,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false);

        /// <summary>
        /// Gets the time series values that match the filtering criteria.
        /// Note: This API is for advanced scenarios only. Use it only when you fetch huge amounts of metrics from
        /// multiple stamps in parallel and face performance problems related to memory usage
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.  By default, all results are returned.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.  By default, automatic resolution is used (same as Jarvis UI).</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.  1 minute resolution (full resolution in MDM today) by default.</param>
        /// <param name="traceId">The trace identifier for the query, used for diagnostic purposes only.  If a trace id is not provided, one will be generated.</param>
        /// <param name="outputDimensionNames">The output dimension names.</param>
        /// <returns>
        /// Time HTTP response
        /// </returns>
        Task<HttpResponseMessage> GetTimeSeriesStreamedAsync(
            MetricIdentifier metricId,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            IReadOnlyList<SamplingType> samplingTypes,
            SelectionClauseV3 selectionClause = null,
            AggregationType aggregationType = AggregationType.Automatic,
            long seriesResolutionInMinutes = 1,
            Guid? traceId = null,
            IReadOnlyList<string> outputDimensionNames = null);

        /// <summary>
        /// Gets the time series values that match the filtering criteria.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.  By default, all results are returned.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.  By default, automatic resolution is used (same as Jarvis UI).</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.  1 minute resolution (full resolution in MDM today) by default.</param>
        /// <param name="traceId">The trace identifier for the query, used for diagnostic purposes only.  If a trace id is not provided, one will be generated.</param>
        /// <param name="outputDimensionNames">The output dimension names.</param>
        /// <param name="lastValueMode">Indicating if the query should be fulfilled with last value mode. If true, null values in the query range requested will be filled with the last known value.</param>
        /// <returns>
        /// Time series definitions matching the query criteria.
        /// </returns>
        Task<IQueryResultListV3> GetTimeSeriesAsync(
            MetricIdentifier metricId,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            IReadOnlyList<SamplingType> samplingTypes,
            SelectionClauseV3 selectionClause = null,
            AggregationType aggregationType = AggregationType.Automatic,
            long seriesResolutionInMinutes = 1,
            Guid? traceId = null,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false);

        /// <summary>
        /// Returns all metric definitions for time series satisfying given set of filters in the given monitoring account.
        /// </summary>
        /// <remarks>
        /// QOS metrics, composite metrics and wild card metrics are not included in the result set.
        /// </remarks>
        /// <param name="monitoringAccount">Monitoring account name.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="traceId">The trace identifier for the query, used for diagnostic purposes only.  If a trace id is not provided, one will be generated.</param>
        /// <returns>
        /// All metric definitions which has data for time series keys satisfy the given set of filters.
        /// </returns>
        Task<IReadOnlyList<MetricDefinitionV2>> GetMetricDefinitionsAsync(
            string monitoringAccount,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            Guid? traceId = null);
    }
}