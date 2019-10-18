// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILocalRawMetric.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An interface representing data for one raw metric.
    /// </summary>
    public interface ILocalRawMetric
    {
        /// <summary>
        /// Gets the monitoring account to which this metric is reported.
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
        /// Gets the metric timestamp.
        /// </summary>
        DateTime MetricTimeUtc { get; }

        /// <summary>
        /// Gets the metric dimensions.
        /// </summary>
        IDictionary<string, string> Dimensions { get; }

        /// <summary>
        /// Gets a value indicating whether the metric is a platform metric.
        /// In such case its value should be taken using property <see cref="MetricDoubleValue" />.
        /// </summary>
        bool IsPlatformMetric { get; }

        /// <summary>
        /// Gets the metric value emitted using metric API.
        /// </summary>
        ulong MetricLongValue { get; }

        /// <summary>
        /// Gets the value of the platform counters.
        /// </summary>
        double MetricDoubleValue { get; }
    }
}