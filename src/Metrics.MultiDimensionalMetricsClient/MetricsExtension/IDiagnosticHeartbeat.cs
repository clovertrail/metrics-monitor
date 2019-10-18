// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDiagnosticHeartbeat.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.MetricsExtension
{
    using System;

    /// <summary>
    /// The interface representing the diagnostic heartbeat event emitted by the MetricsExtension over ETW.
    /// </summary>
    public interface IDiagnosticHeartbeat
    {
        /// <summary>
        /// Gets the name of the MetricsExtension instance this heartbeat was emitted from.
        /// </summary>
        string InstanceName { get; }

        /// <summary>
        /// Gets the uptime in seconds.
        /// </summary>
        int UptimeInSec { get; }

        /// <summary>
        /// Gets the count of ETW events that were successfully received by the extension but could not be processed and were dropped.
        /// </summary>
        int EtwEventsDroppedCount { get; }

        /// <summary>
        /// Gets the count of ETW events that were lost prior to being recieved by the extension.
        /// </summary>
        int EtwEventsLostCount { get; }

        /// <summary>
        /// Gets the count of aggregated metrics that were dropped prior to publication.
        /// </summary>
        int AggregatedMetricsDroppedCount { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is nearing reaching the maximum ETW processing queue size.
        /// </summary>
        bool IsNearingEtwQueueLimit { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is nearing reaching the maximum aggregation queue limit for any of the publishers.
        /// </summary>
        bool IsNearingAggregationQueueLimit { get; }
    }
}
