// -----------------------------------------------------------------------
// <copyright file="IMetricMetadata.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;

    /// <summary>
    /// Represents an interface for the metric metadata object.
    /// </summary>
    public interface IMetricMetadata
    {
        /// <summary>
        /// Gets the namespace of the metric.
        /// </summary>
        string MetricNamespace { get; }

        /// <summary>
        /// Gets the name of the metric.
        /// </summary>
        string MetricName { get; }

        /// <summary>
        /// Gets the number of dimensions the metric has.
        /// </summary>
        int DimensionsCount { get; }

        /// <summary>
        /// Gets the name of the dimension by index.
        /// </summary>
        /// <param name="dimensionIndex">Index of the dimension in 0..DimensionsCount-1 range.</param>
        /// <returns>Name of the dimension.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is out of the specified range.</exception>
        string GetDimensionName(int dimensionIndex);
    }
}
