// -----------------------------------------------------------------------
// <copyright file="IMetricBuilder.cs" company="Microsoft Corporation">
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
    public interface IMetricBuilder<TMetadata>
        where TMetadata : IMetricMetadata
    {
        /// <summary>
        /// Sets the packet serialization version.
        /// </summary>
        /// <param name="serializationVersion">Serialization version.</param>
        void SetSerializationVersion(ushort serializationVersion);

        /// <summary>
        /// Creates the custom object representing metric metadata. Note that the same metadata object can be shared
        /// by many metric instances.
        /// </summary>
        /// <param name="metricNamespace">Namespace of the metric.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="dimensionNames">Names of the metric dimensions.</param>
        /// <returns>The metric metadata representing the given parameters.</returns>
        TMetadata CreateMetadata(string metricNamespace, string metricName, IEnumerable<string> dimensionNames);

        /// <summary>
        /// Signals to the builder that it should get ready to start filling up data for a new metric.
        /// </summary>
        void BeginMetricCreation();

        /// <summary>
        /// Assigns metadata to the metric being built.
        /// </summary>
        /// <param name="metadata">Metric metadata.</param>
        void AssignMetadata(TMetadata metadata);

        /// <summary>
        /// Assigns a monitoring account to the metric being built. This is only called if the aggregated metric data was serialized
        /// with a version prior to v3. For v3 and above the monitoring account should be captured on the request itself, as part of the
        /// URL path or as a query parameter.
        /// </summary>
        /// <param name="value">
        /// Monitoring account associated with the metric.
        /// </param>
        /// <remarks>
        /// If we ever make this interface public we should check if there are no more clients submitting data prior to v3 and remove
        /// this method from the interface to avoid confusion from the users.
        /// </remarks>
        void AssignMonitoringAccount(string value);

        /// <summary>
        /// Assigns a namespace to the metric being built.
        /// </summary>
        /// <param name="value">Namespace associated with the metric.</param>
        void AssignNamespace(string value);

        /// <summary>
        /// Assigns time (UTC) to the metric being built.
        /// </summary>
        /// <param name="value">Time of the metric.</param>
        void AssignTimeUtc(DateTime value);

        /// <summary>
        /// Adds the value of a single dimension to the metric being built. The dimension values are passed in the same
        /// order as the corresponding dimensions on the metric metadata.
        /// </summary>
        /// <param name="value">Value of one dimension of the metric.</param>
        void AddDimensionValue(string value);

        /// <summary>
        /// Assigns the sampling types of the metric being built.
        /// </summary>
        /// <param name="value">Available sampling types of the metric.</param>
        void AssignSamplingTypes(SamplingTypes value);

        /// <summary>
        /// Assigns the minimum value of the metric being built.
        /// </summary>
        /// <param name="value">Minimum value of the metric.</param>
        void AssignMin(ulong value);

        /// <summary>
        /// Assigns the maximum value of the metric being built.
        /// </summary>
        /// <param name="value">Maximum value of the metric.</param>
        void AssignMax(ulong value);

        /// <summary>
        /// Assigns the sum value of the metric being built.
        /// </summary>
        /// <param name="value">Sum value of the metric.</param>
        void AssignSum(ulong value);

        /// <summary>
        /// Assigns the sum of square differences from mean value of the metric being built.
        /// </summary>
        /// <param name="value">Sum of square differences from mean of the value of the metric.</param>
        void AssignSumOfSquareDiffFromMean(double value);

        /// <summary>
        /// Assigns the count (i.e.: how many times it was logged) value of the metric being built.
        /// </summary>
        /// <param name="value">Count value of the metric.</param>
        void AssignCount(uint value);

        /// <summary>
        /// Assigns a histogram to the metric being built.
        /// </summary>
        /// <param name="value">Histogram of the metric.</param>
        void AssignHistogram(IReadOnlyList<KeyValuePair<ulong, uint>> value);

        /// <summary>
        /// Assigns hyperloglogsketches to the metric being built.
        /// </summary>
        /// <param name="reader">Stream containing the data.</param>
        /// <param name="length">Length of data to read.</param>
        void AssignHyperLogLogSketch(BinaryReader reader, int length);

        /// <summary>
        /// Signals to the builder that the creation of the current metric was completed.
        /// </summary>
        void EndMetricCreation();
    }
}
