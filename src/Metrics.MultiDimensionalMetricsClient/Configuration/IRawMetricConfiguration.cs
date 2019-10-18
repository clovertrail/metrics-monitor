//-------------------------------------------------------------------------------------------------
// <copyright file="IRawMetricConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System.Collections.Generic;
    using Metrics;

    /// <summary>
    /// Represents a raw metric in MDM.
    /// </summary>
    public interface IRawMetricConfiguration : IMetricConfiguration
    {
        /// <summary>
        /// Gets or sets the scaling factor.
        /// </summary>
        float? ScalingFactor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether client publication is enabled.
        /// </summary>
        bool EnableClientPublication { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether client forking is enabled.
        /// </summary>
        bool EnableClientForking { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the metric will be published to the aggregated ETW channel.
        /// </summary>
        bool EnableClientEtwPublication { get; set; }

        /// <summary>
        /// Gets the raw sampling types (Sum, Count, or legacy MetricsClient sampling types).
        /// </summary>
        IEnumerable<SamplingType> RawSamplingTypes { get; }

        /// <summary>
        /// Gets the preaggregations of the metric.
        /// </summary>
        IEnumerable<IPreaggregation> Preaggregations { get; }

        /// <summary>
        /// Gets the dimensions of the metric.
        /// </summary>
        IEnumerable<string> Dimensions { get; }

        /// <summary>
        /// Gets the computed sampling types.
        /// </summary>
        IEnumerable<IComputedSamplingTypeExpression> ComputedSamplingTypes { get; }

        /// <summary>
        /// Gets or sets a value indicating whether only the last value seen for a time series is perserved on the client.
        /// </summary>
        /// <value>
        /// <c>true</c> if last sampling mode is used; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Client side last sampling mode means that within the collection interval (1m) only the last value set to the metric is kept.  This means Sum == Min == Max
        /// and Count == 1 for this metric when it is sent to the server.
        /// </remarks>
        bool EnableClientSideLastSamplingMode { get; }

        /// <summary>
        /// Determines whether this instance can add preaggregation to the metric configuration.
        /// </summary>
        /// <param name="preaggregationToAdd">The preaggregation to add.</param>
        /// <returns>True if the preaggregation can be added.</returns>
        bool CanAddPreaggregation(IPreaggregation preaggregationToAdd);

        /// <summary>
        /// Adds the preaggregate.
        /// </summary>
        /// <param name="preaggregate">The preaggregate.</param>
        void AddPreaggregation(IPreaggregation preaggregate);

        /// <summary>
        /// Removes the preaggregate.
        /// </summary>
        /// <param name="preaggregateName">The name of the preaggregate.</param>
        void RemovePreaggregation(string preaggregateName);

        /// <summary>
        /// Adds the type of the computed sampling.
        /// </summary>
        /// <param name="computedSamplingType">Type of the computed sampling.</param>
        void AddComputedSamplingType(IComputedSamplingTypeExpression computedSamplingType);

        /// <summary>
        /// Removes the type of the computed sampling.
        /// </summary>
        /// <param name="computedSamplingTypeName">Name of the computed sampling type.</param>
        void RemoveComputedSamplingType(string computedSamplingTypeName);
    }
}
