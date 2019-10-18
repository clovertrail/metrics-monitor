//-------------------------------------------------------------------------------------------------
// <copyright file="RawMetricConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Metrics;
    using Newtonsoft.Json;
    using Online.Metrics.Serialization;

    /// <summary>
    /// Represents a raw metric in MDM.
    /// </summary>
    public sealed class RawMetricConfiguration : IRawMetricConfiguration
    {
        private readonly List<IPreaggregation> preaggregations;
        private readonly List<IComputedSamplingTypeExpression> computedSamplingTypes;
        private string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="RawMetricConfiguration"/> class.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="name">The name.</param>
        /// <param name="lastUpdatedTime">The last updated time.</param>
        /// <param name="lastUpdatedBy">The last updated by.</param>
        /// <param name="version">The version.</param>
        /// <param name="scalingFactor">The scaling factor.</param>
        /// <param name="enableClientPublication">if set to <c>true</c> to enable client publication.</param>
        /// <param name="enableClientForking">if set to <c>true</c> to enable client forking.</param>
        /// <param name="description">The description of the metric.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="preaggregations">The preaggregations.</param>
        /// <param name="rawSamplingTypes">The raw sampling types.</param>
        /// <param name="computedSamplingTypes">The computed sampling types.</param>
        /// <param name="useClientSideLastSamplingMode">Whether or not to only use the last value written within the sample period.</param>
        /// <param name="useClientSideEtwPublication">Whether or not the metric should be published to the aggregated ETW provider.</param>
        [JsonConstructor]
        internal RawMetricConfiguration(
            string metricNamespace,
            string name,
            DateTime lastUpdatedTime,
            string lastUpdatedBy,
            uint version,
            float? scalingFactor,
            bool enableClientPublication,
            bool enableClientForking,
            string description,
            IEnumerable<string> dimensions,
            IEnumerable<IPreaggregation> preaggregations,
            IEnumerable<SamplingType> rawSamplingTypes,
            IEnumerable<IComputedSamplingTypeExpression> computedSamplingTypes,
            bool useClientSideLastSamplingMode,
            bool useClientSideEtwPublication)
        {
            this.MetricNamespace = metricNamespace;
            this.Name = name;
            this.LastUpdatedTime = lastUpdatedTime;
            this.LastUpdatedBy = lastUpdatedBy;
            this.Version = version;
            this.ScalingFactor = scalingFactor;
            this.EnableClientPublication = enableClientPublication;
            this.EnableClientForking = enableClientForking;
            this.Description = description;
            this.Dimensions = dimensions;
            this.preaggregations = preaggregations.ToList();
            this.RawSamplingTypes = rawSamplingTypes;
            this.computedSamplingTypes = computedSamplingTypes.ToList();
            this.EnableClientSideLastSamplingMode = useClientSideLastSamplingMode;
            this.EnableClientEtwPublication = useClientSideEtwPublication;
        }

        /// <summary>
        /// The namespace of the metric.
        /// </summary>
        public string MetricNamespace { get; }

        /// <summary>
        /// The name of the metric.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The last updated time of the metric.
        /// </summary>
        public DateTime LastUpdatedTime { get; }

        /// <summary>
        /// The last entity to update the metric.
        /// </summary>
        public string LastUpdatedBy { get; }

        /// <summary>
        /// The version of the metric.
        /// </summary>
        public uint Version { get; }

        /// <summary>
        /// Gets or sets the scaling factor.
        /// </summary>
        public float? ScalingFactor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether client publication is enabled.
        /// </summary>
        public bool EnableClientPublication { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether client forking is enabled.
        /// </summary>
        public bool EnableClientForking { get; set; }

        /// <summary>
        /// Gets the description of the metric.
        /// </summary>
        public string Description
        {
            get
            {
                return this.description;
            }

            set
            {
                if (value != null && value.Length > SerializationConstants.MaximumMetricDescriptionLength)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        $"The metric description cannot be greater than {SerializationConstants.MaximumMetricDescriptionLength} characters.");
                }

                this.description = value;
            }
        }

        /// <summary>
        /// Gets the raw sampling types (Sum, Count, or legacy MetricsClient sampling types).
        /// </summary>
        public IEnumerable<SamplingType> RawSamplingTypes { get; }

        /// <summary>
        /// Gets the preaggregations of the metric.
        /// </summary>
        public IEnumerable<IPreaggregation> Preaggregations
        {
            get { return this.preaggregations; }
        }

        /// <summary>
        /// Gets the dimensions of the metric.
        /// </summary>
        public IEnumerable<string> Dimensions { get; }

        /// <summary>
        /// Gets the computed sampling types.
        /// </summary>
        public IEnumerable<IComputedSamplingTypeExpression> ComputedSamplingTypes
        {
            get { return this.computedSamplingTypes; }
        }

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
        [JsonProperty]
        public bool EnableClientSideLastSamplingMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the metric will be published to the aggregated ETW channel.
        /// </summary>
        public bool EnableClientEtwPublication { get; set; }

        /// <summary>
        /// Determines whether this instance can add preaggregation to the metric configuration.
        /// </summary>
        /// <param name="preaggregationToAdd">The preaggregation to add.</param>
        /// <returns>
        /// True if the preaggregation can be added.
        /// </returns>
        public bool CanAddPreaggregation(IPreaggregation preaggregationToAdd)
        {
            if (preaggregationToAdd == null)
            {
                throw new ArgumentNullException(nameof(preaggregationToAdd));
            }

            foreach (var preaggregation in this.preaggregations)
            {
                if (string.Equals(preaggregationToAdd.Name, preaggregation.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (preaggregation.Dimensions.SequenceEqual(preaggregationToAdd.Dimensions, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds the preaggregate.
        /// </summary>
        /// <param name="preaggregate">The preaggregate.</param>
        public void AddPreaggregation(IPreaggregation preaggregate)
        {
            if (!this.CanAddPreaggregation(preaggregate))
            {
                throw new ConfigurationValidationException("Duplicate preaggregates cannot be added.", ValidationType.DuplicatePreaggregate);
            }

            this.preaggregations.Add(preaggregate);
        }

        /// <summary>
        /// Removes the preaggregate.
        /// </summary>
        /// <param name="preaggregateName">The name of the preaggregate to remove.</param>
        public void RemovePreaggregation(string preaggregateName)
        {
            if (string.IsNullOrWhiteSpace(preaggregateName))
            {
                throw new ArgumentNullException(nameof(preaggregateName));
            }

            this.preaggregations.RemoveAll(x => string.Equals(x.Name, preaggregateName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds the type of the computed sampling.
        /// </summary>
        /// <param name="computedSamplingType">Type of the computed sampling.</param>
        public void AddComputedSamplingType(IComputedSamplingTypeExpression computedSamplingType)
        {
            if (this.computedSamplingTypes.Any(x => x.Name.Equals(computedSamplingType.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConfigurationValidationException("Duplicate computed sampling types cannot be added.", ValidationType.DuplicateSamplingType);
            }

            this.computedSamplingTypes.Add(computedSamplingType);
        }

        /// <summary>
        /// Removes the type of the computed sampling.
        /// </summary>
        /// <param name="computedSamplingTypeName">Name of the computed sampling type.</param>
        public void RemoveComputedSamplingType(string computedSamplingTypeName)
        {
            if (string.IsNullOrWhiteSpace(computedSamplingTypeName))
            {
                throw new ArgumentNullException(nameof(computedSamplingTypeName));
            }

            for (var i = 0; i < this.computedSamplingTypes.Count; ++i)
            {
                if (string.Equals(computedSamplingTypeName, this.computedSamplingTypes[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.computedSamplingTypes[i].IsBuiltIn)
                    {
                        throw new ConfigurationValidationException("Built in computed sampling types cannot be removed.", ValidationType.BuiltInTypeRemoved);
                    }

                    this.computedSamplingTypes.RemoveAt(i);

                    return;
                }
            }
        }
    }
}
