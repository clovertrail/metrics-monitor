// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILocalMetricReader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The interface for consumption of locally aggregated metrics or local raw metrics.
    /// </summary>
    public interface ILocalMetricReader
    {
        /// <summary>
        /// Reads the local raw metrics.
        /// </summary>
        /// <param name="metricProducedAction">The action to execute when metric available.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="etlFileName">The name of the etw file from when read data. If null, realtime session will be used.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task ReadLocalRawMetricsAsync(
            Action<ILocalRawMetric> metricProducedAction,
            CancellationToken cancellationToken,
            string etlFileName = null);

        /// <summary>
        /// Reads the locally aggregated metrics.
        /// </summary>
        /// <param name="metricProducedAction">The action to execute when metric available.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="etlFileName">The name of the etw file from when read data. If null, realtime session will be used.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task ReadLocalAggregatedMetricsAsync(
            Action<ILocalAggregatedMetric> metricProducedAction,
            CancellationToken cancellationToken,
            string etlFileName = null);
    }
}