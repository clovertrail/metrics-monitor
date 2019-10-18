// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IReadOnlyMetric.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <author email="selavrin">
//     Sergii Lavrinenko
// </author>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;
    using System.IO;
    using global::Metrics.Services.Common.BlobSegment;

    /// <summary>
    /// Read only interface for metric data.
    /// </summary>
    /// TODO: Once all publishers and recievers are moved to version 3 of serialization remove MonitoringAccount and MetricNamespace from here
    public interface IReadOnlyMetric
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
        /// Gets the time when metric was reported.
        /// </summary>
        DateTime TimeUtc { get; }

        /// <summary>
        /// Gets the metric metadata.
        /// </summary>
        IMetricMetadata MetricMetadata { get; }

        /// <summary>
        /// Gets the sampling types this metric contains.
        /// </summary>
        SamplingTypes SamplingTypes { get; }

        /// <summary>
        /// Gets the number of samples for which this metric is reported.
        /// </summary>
        uint Count { get; }

        /// <summary>
        /// Gets the minimum value of samples reported this metric.
        /// </summary>
        ulong Min { get; }

        /// <summary>
        /// Gets the maximum value of samples reported this metric.
        /// </summary>
        ulong Max { get; }

        /// <summary>
        /// Gets the sum of sample values reported this metric.
        /// </summary>
        ulong Sum { get; }

        /// <summary>
        /// Gets the sum of squares differences from the mean for the sample values reported this metric.
        /// </summary>
        double SumOfSquareDiffFromMean { get; }

        /// <summary>
        /// Gets the minimum value of samples reported this metric.
        /// </summary>
        MetricValueV2 MinUnion { get; }

        /// <summary>
        /// Gets the maximum value of samples reported this metric.
        /// </summary>
        MetricValueV2 MaxUnion { get; }

        /// <summary>
        /// Gets the sum of sample values reported this metric.
        /// </summary>
        MetricValueV2 SumUnion { get; }

        /// <summary>
        /// Gets the histogram created from sample values reported for this metric.
        /// </summary>
        IReadOnlyHistogram Histogram { get; }

        /// <summary>
        /// Gets the tdigest created from sample values reported for this metric.
        /// </summary>
        IReadOnlyTDigest TDigest { get; }

        /// <summary>
        /// Gets the hyperloglog sketches from this metric.
        /// </summary>
        IReadOnlyHyperLogLogSketches HyperLogLogSketches { get; }

        /// <summary>
        /// Gets the hyperloglog sketches stream.
        /// </summary>
        /// <remarks>
        /// This will be null if HyperLogLogSketches is set and vice-versa.
        /// </remarks>
        Stream HyperLogLogSketchesStream { get; }

        /// <summary>
        /// Gets the value of the dimension by dimension index.
        /// Implementer of this interface has to make sure that number of dimensions specified in MetricMetadata be equal to number of dimension values.
        /// </summary>
        /// <param name="dimensionIndex">Index of the dimension for which to get value in 0..MetricMetadata.DimensionsCount range.</param>
        /// <returns>Value of the dimension.</returns>
        string GetDimensionValue(int dimensionIndex);
    }
}
