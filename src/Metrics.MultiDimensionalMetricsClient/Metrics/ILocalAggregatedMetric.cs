// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILocalAggregatedMetric.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The interface representing the locally aggregated metric in the ETW stream.
    /// </summary>
    public interface ILocalAggregatedMetric
    {
        /// <summary>
        /// Gets the Monitoring Account to which this metric is reported.
        /// </summary>
        string MonitoringAccount { get; }

        /// <summary>
        /// Gets the metric namespace.
        /// </summary>
        string MetricNamespace { get; }

        /// <summary>
        /// Gets the metric name.
        /// </summary>
        string MetricName { get; }

        /// <summary>
        /// Gets the time in UTC when metric was reported.
        /// </summary>
        DateTime MetricTimeUtc { get; }

        /// <summary>
        /// Gets the dimension name-value dictionary.
        /// </summary>
        /// <remarks>The dimension names are case insensitive.</remarks>
        IReadOnlyDictionary<string, string> Dimensions { get; }

        /// <summary>
        /// Gets the scaling factor applied to metric values.
        /// </summary>
        float ScalingFactor { get; }

        /// <summary>
        /// Gets the number of samples for which this metric is reported.
        /// </summary>
        uint Count { get; }

        /// <summary>
        /// Gets the scaled sum of sample values reported this metric.
        /// </summary>
        float ScaledSum { get; }

        /// <summary>
        /// Gets the scaled minimum value of samples reported this metric.
        /// </summary>
        float ScaledMin { get; }

        /// <summary>
        /// Gets the scaled maximum value of samples reported this metric.
        /// </summary>
        float ScaledMax { get; }

        /// <summary>
        /// Gets the sum of sample values reported this metric.
        /// </summary>
        ulong Sum { get; }

        /// <summary>
        /// Gets the minimum value of samples reported this metric.
        /// </summary>
        ulong Min { get; }

        /// <summary>
        /// Gets the maximum value of samples reported this metric.
        /// </summary>
        ulong Max { get; }
    }
}