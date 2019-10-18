//-------------------------------------------------------------------------------------------------
// <copyright file="IMetricConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;

    /// <summary>
    /// Interface representing an MDM Metric.
    /// </summary>
    public interface IMetricConfiguration
    {
        /// <summary>
        /// The namespace of the metric.
        /// </summary>
        string MetricNamespace { get; }

        /// <summary>
        /// The name of the metric.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The last updated time of the metric.
        /// </summary>
        DateTime LastUpdatedTime { get; }

        /// <summary>
        /// The last entity to update the metric.
        /// </summary>
        string LastUpdatedBy { get; }

        /// <summary>
        /// The version of the metric.
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Gets the description of the metric.
        /// </summary>
        string Description { get; }
    }
}
