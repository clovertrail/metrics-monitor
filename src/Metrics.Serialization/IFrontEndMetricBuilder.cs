// -----------------------------------------------------------------------
// <copyright file="IFrontEndMetricBuilder.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// The interface used by deserializer to abstract the way how deserialized objects are created.
    /// It is up to the user of the deserializer to implement type to which data are deserialized, use object pools or not etc.
    /// </summary>
    /// <typeparam name="TMetadata">Type of metadata to use for creation of metric data objects.</typeparam>
    public interface IFrontEndMetricBuilder<TMetadata>
        where TMetadata : IMetricMetadata
    {
        /// <summary>
        /// Sets the packet serialization version.
        /// </summary>
        /// <param name="serializationVersion">Serialization version.</param>
        void SetSerializationVersion(ushort serializationVersion);

        /// <summary>
        /// Gets interned string for given value.
        /// </summary>
        /// <param name="value">String to be interned.</param>
        /// <returns>Interned string.</returns>
        string GetString(string value);

        /// <summary>
        /// Creates the custom object representing metric metadata. Note that the same metadata object can be shared
        /// by many metric instances.
        /// </summary>
        /// <param name="metricNamespace">Namespace of the metric.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="dimensionNames">Names of the metric dimensions.</param>
        /// <returns>The metric metadata representing the given parameters.</returns>
        TMetadata CreateMetadata(string metricNamespace, string metricName, IReadOnlyList<string> dimensionNames);

        /// <summary>
        /// Start adding metric data with populating common values.
        /// </summary>
        /// <param name="metadata">Metric metadata.</param>
        /// <param name="dimensionValues">List of dimension values.</param>
        /// <param name="timeUtc">Time of the metric.</param>
        /// <param name="samplingTypes">Available sampling types of the metric.</param>
        /// <param name="count">Count value of the metric (how many times it was reported).</param>
        /// <param name="sum">Sum value of the metric.</param>
        /// <param name="min">Minimum value of the metric.</param>
        /// <param name="max">Maximum value of the metric.</param>
        /// <param name="sumOfSquareDiffFromMean">Sum of squares differences from mean of the metric.</param>
        void BeginMetricCreation(
            TMetadata metadata,
            IReadOnlyList<string> dimensionValues,
            DateTime timeUtc,
            SamplingTypes samplingTypes,
            uint count,
            MetricValueV2 sum,
            MetricValueV2 min,
            MetricValueV2 max,
            double sumOfSquareDiffFromMean);

        /// <summary>
        /// Assigns a histogram to the metric being built.
        /// </summary>
        /// <param name="value">Histogram of the metric.</param>
        void AssignHistogram(IReadOnlyList<KeyValuePair<ulong, uint>> value);

        /// <summary>
        /// Assigns a tdigest to the metric being built
        /// </summary>
        /// <param name="reader">Reader containing the data.</param>
        /// <param name="length">Length of data to read.</param>
        void AssignTDigest(BinaryReader reader, int length);

        /// <summary>
        /// Assigns hyperloglogsketches to the metric being built.
        /// </summary>
        /// <param name="reader">Stream containing the data.</param>
        /// <param name="length">Length of data to read.</param>
        void AssignHyperLogLogSketch(BinaryReader reader, int length);

        /// <summary>
        /// Signals that deserializer has completed metric deserialization.
        /// </summary>
        void EndMetricCreation();
    }
}
