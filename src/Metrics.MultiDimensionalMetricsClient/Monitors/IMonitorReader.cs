// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMonitorReader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Online.Metrics.Serialization.Configuration;
    using Microsoft.Online.Metrics.Serialization.Monitor;

    /// <summary>
    /// The interface to query monitor health status.
    /// </summary>
    public interface IMonitorReader
    {
        /// <summary>
        /// Gets the monitors for the given <paramref name="metricIdentifier"/>.
        /// </summary>
        /// <param name="metricIdentifier">The metric identifier.</param>
        /// <returns>The monitors for the given <paramref name="metricIdentifier"/>.</returns>
        Task<IReadOnlyList<MonitorIdentifier>> GetMonitorsAsync(MetricIdentifier metricIdentifier);

        /// <summary>
        /// Gets the monitor IDs for the given monitoring account, optionally with the metric namespace.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>
        /// The monitor IDs for the given monitoring account, optionally with the metric namespace.
        /// </returns>
        Task<IReadOnlyList<MonitorIdentifier>> GetMonitorsAsync(string monitoringAccount, string metricNamespace = null);

        /// <summary>
        /// Gets the current heath status asynchronous.
        /// </summary>
        /// <param name="monitorInstanceDefinition">The monitor instance definition.</param>
        /// <returns>Monitor health status.</returns>
        [System.Obsolete("Deprecated, please use GetCurrentHealthStatusAsync (notice fixed spelling) instead.")]
        Task<IMonitorHealthStatus> GetCurrentHeathStatusAsync(TimeSeriesDefinition<MonitorIdentifier> monitorInstanceDefinition);

        /// <summary>
        /// Gets the current monitor health status.
        /// </summary>
        /// <param name="monitorInstanceDefinition">The monitor instance definition.</param>
        /// <returns>
        /// The current monitor health status.
        /// </returns>
        Task<IMonitorHealthStatus> GetCurrentHealthStatusAsync(TimeSeriesDefinition<MonitorIdentifier> monitorInstanceDefinition);

        /// <summary>
        /// Batched API to get the current monitor health statuses.
        /// </summary>
        /// <param name="monitorInstanceDefinitions">The monitor instance definitions.</param>
        /// <returns>
        /// The current monitor health statuses.
        /// </returns>
        Task<IReadOnlyList<KeyValuePair<TimeSeriesDefinition<MonitorIdentifier>, IMonitorHealthStatus>>> GetMultipleCurrentHeathStatusesAsync(
            params TimeSeriesDefinition<MonitorIdentifier>[] monitorInstanceDefinitions);

        /// <summary>
        /// Batched API to get the current monitor health statuses.
        /// </summary>
        /// <param name="monitorInstanceDefinitions">The monitor instance definitions.</param>
        /// <returns>
        /// The current monitor health statuses.
        /// </returns>
        Task<IReadOnlyList<KeyValuePair<TimeSeriesDefinition<MonitorIdentifier>, IMonitorHealthStatus>>> GetMultipleCurrentHeathStatusesAsync(
            IEnumerable<TimeSeriesDefinition<MonitorIdentifier>> monitorInstanceDefinitions);

        /// <summary>
        /// Gets the monitor history.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="monitorInstanceDefinition">The monitor instance definition.</param>
        /// <returns>
        /// The monitor health history for each minute in the provided time range.
        /// true means healthy, false means unhealthy, and null means that the monitor didn't report a health status for that minute.
        /// </returns>
        [Obsolete("We are going to retire this. Please use GetBatchWatchdogHealthHistory in Health SDK instead.")]
        Task<TimeSeries<MonitorIdentifier, bool?>> GetMonitorHistoryAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            TimeSeriesDefinition<MonitorIdentifier> monitorInstanceDefinition);
    }
}