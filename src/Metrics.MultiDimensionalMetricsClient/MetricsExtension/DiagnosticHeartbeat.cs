// -----------------------------------------------------------------------
// <copyright file="DiagnosticHeartbeat.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.MetricsExtension
{
    using Metrics.Etw;

    /// <summary>
    /// A class that represents the extension diagnostic heartbeat.
    /// </summary>
    internal sealed class DiagnosticHeartbeat : IDiagnosticHeartbeat
    {
        /// <summary>
        /// Gets the count of aggregated metrics that were dropped prior to publication.
        /// </summary>
        public int AggregatedMetricsDroppedCount { get; private set; }

        /// <summary>
        /// Gets the count of ETW events that were successfully received by the extension but could not be processed and were dropped.
        /// </summary>
        public int EtwEventsDroppedCount { get; private set; }

        /// <summary>
        /// Gets the count of ETW events that were lost prior to being recieved by the extension.
        /// </summary>
        public int EtwEventsLostCount { get; private set; }

        /// <summary>
        /// Gets the name of the MetricsExtension instance this heartbeat was emitted from.
        /// </summary>
        public string InstanceName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is nearing reaching the maximum aggregation queue limit for any of the publishers.
        /// </summary>
        public bool IsNearingAggregationQueueLimit { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is nearing reaching the maximum ETW processing queue size.
        /// </summary>
        public bool IsNearingEtwQueueLimit { get; private set; }

        /// <summary>
        /// Gets the uptime in seconds.
        /// </summary>
        public int UptimeInSec { get; private set; }

        /// <summary>
        /// Froms the etw event.
        /// </summary>
        /// <param name="etwMetricData">The etw metric data.</param>
        /// <returns>The diagnostic heartbeat.</returns>
        public static unsafe IDiagnosticHeartbeat FromEtwEvent(NativeMethods.EventRecord* etwMetricData)
        {
            var heartbeat = new DiagnosticHeartbeat();

            var pointerInPayload = etwMetricData->UserData;
            heartbeat.InstanceName = EtwPayloadManipulationUtils.ReadString(ref pointerInPayload);

            var uptimeSec = *((int*)pointerInPayload);
            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(int));
            heartbeat.UptimeInSec = uptimeSec;

            var etwEventsDropped = *((int*)pointerInPayload);
            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(int));
            heartbeat.EtwEventsDroppedCount = etwEventsDropped;

            var etwEventsLost = *((int*)pointerInPayload);
            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(int));
            heartbeat.EtwEventsLostCount = etwEventsLost;

            var aggregatedMetricsDropped = *((int*)pointerInPayload);
            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(int));
            heartbeat.AggregatedMetricsDroppedCount = aggregatedMetricsDropped;

            var isNearingEtwLimit = *((byte*)pointerInPayload);
            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(byte));
            heartbeat.IsNearingEtwQueueLimit = isNearingEtwLimit != 0;

            var isNearingAggregationLimit = *((byte*)pointerInPayload);
            heartbeat.IsNearingAggregationQueueLimit = isNearingAggregationLimit != 0;

            return heartbeat;
        }
    }
}
