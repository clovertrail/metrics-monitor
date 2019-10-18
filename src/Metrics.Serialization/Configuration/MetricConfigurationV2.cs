//-------------------------------------------------------------------------------------------------
// <copyright file="MetricConfigurationV2.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Newtonsoft.Json;

    /// <summary>
    /// The configuration used by MetricsExtension.  It will only contain the necessary
    /// fields needed by the client.
    /// </summary>
    public struct MetricConfigurationV2 : IEquatable<MetricConfigurationV2>
    {
        /// <summary>
        /// The name of the metric used for pumping data for <see cref="SamplingTypes.Count"/>
        /// in the new (Monitoring Agent) pipeline.
        /// </summary>
        public const string CountMetricName = "Count";

        /// <summary>
        /// The name of the metric used for pumping data for <see cref="SamplingTypes.Sum"/>
        /// in the new (Monitoring Agent) pipeline.
        /// </summary>
        /// <remarks>
        /// This is the metric contains configuration indicating whether <see cref="SamplingTypes.Max"/>,
        /// <see cref="SamplingTypes.Min"/>, <see cref="Histogram"/> etc.. are enabled.
        /// </remarks>
        public const string SumMetricName = "Sum";

        /// <summary>
        /// Gets the default configuration to be used, when the configuration is not obtained from server.
        /// </summary>
        public const SamplingTypes DefaultSamplingTypes = SamplingTypes.Count | SamplingTypes.Sum;

        /// <summary>
        /// Gets or sets the collections of dimensions configured for Distinct count.
        /// </summary>
        [JsonProperty("pc")]
        public List<PreAggregateConfiguration> PreAggregationsWithDistinctCountColumns;

        /// <summary>
        /// The map of "special" strings in metric names to default scaling factors to be used when scaling factor is not configured for metrics.
        /// </summary>
        /// <remarks>
        /// The values for scaling factor in this list must be greater than or equal to zero.
        /// </remarks>
        private static readonly List<KeyValuePair<string, float>> DefaultScalingFactorForMetrics =
            new List<KeyValuePair<string, float>> { new KeyValuePair<string, float>("%", 100f), };

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricConfigurationV2"/> struct.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="scalingFactor">The scaling factor.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        public MetricConfigurationV2(string metricName, float? scalingFactor, SamplingTypes samplingTypes)
            : this()
        {
            this.MetricName = metricName;
            this.ScalingFactor = scalingFactor;
            this.SamplingTypes = samplingTypes;

            this.EnableClientSideForking = true;
            this.EnableClientSidePublication = true;
            this.EnableClientSideEtwPublication = true;
            this.IngestionOptions = 0;
            this.HyperLogLogBValue = HyperLogLogSketch.DefaultBValue;
        }

        /// <summary>
        /// Gets or sets the version of this instance.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public uint Version { get; set; }

        /// <summary>
        /// Gets or sets the Monitoring Account to which this metric is reported.
        /// </summary>
        [JsonProperty(PropertyName = "ma", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MonitoringAccount { get; set; }

        /// <summary>
        /// Gets or sets the metric namespace.
        /// </summary>
        [JsonProperty(PropertyName = "ns", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MetricNamespace { get; set; }

        /// <summary>
        /// Gets or sets the metric name.
        /// </summary>
        [JsonProperty(PropertyName = "m")]
        public string MetricName { get; set; }

        /// <summary>
        /// Gets the metric name and sampling types.
        /// </summary>
        [JsonProperty(PropertyName = "s", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(DefaultSamplingTypes)]
        public SamplingTypes SamplingTypes { get; set; }

        /// <summary>
        /// Gets or sets the scaling factor to be used.
        /// </summary>
        /// <remarks>
        /// Clients should pass un-scaled values when this is null.
        /// </remarks>
        [JsonProperty(PropertyName = "sf", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float? ScalingFactor { get; set; }

        /// <summary>
        /// Gets or sets the collections of dimensions configured for Distinct count.
        /// </summary>
        /// <remarks>
        /// Each HyperLogLog counter uses a small, fixed amount of space but can estimate
        /// the cardinality of any set of up to around a billion values with relative error
        /// of 1.04 / Math.sqrt(2 ** b) with high probability.
        /// </remarks>
        [JsonProperty("bv")]
        public int HyperLogLogBValue { get; set; }

        /// <summary>
        /// Gets or sets the list of dimension configurations.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<DimensionConfiguration> DimensionConfigurations { get; set; }

        /// <summary>
        /// Gets or sets the list of pre-aggregates which this metric will be aggregated with.
        /// </summary>
        /// <remarks>
        /// We have hidden the configuration APIs.
        /// If we ever need to expose the configuration APIs, we need to revisit the types of properties.
        /// For this particular property, we can expose a strong-typed class named PreAggregateConfigurationList that can check for duplicate entries,
        /// or we can even make the whole configuration object read only.
        /// </remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<PreAggregateConfiguration> PreAggregations { get; set; }

        /// <summary>
        /// Gets or sets the computed sampling types.
        /// In the case of <see cref="IsCompositeMetric"/>, this field is used to save the expressions and their names.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<ComputedSamplingTypeConfiguration> ComputedSamplingTypes { get; set; }

        /// <summary>
        /// Gets or sets the list of the variable names of metrics to the metric identifiers.
        /// The keys can be used in the <see cref="ComputedSamplingTypeConfiguration.Expression"/> field of the <see cref="ComputedSamplingTypes"/>.
        /// </summary>
        /// <remarks>
        /// Used only for composite metrics.
        /// Composite metric cannot use <see cref="MetricIdentifier"/> of another composite metric as of now, so the UI should show hints appropriately.
        /// </remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, MetricIdentifier> MetricIdentifiers { get; set; }

        /// <summary>
        /// Gets a value indicating whether the metric configuration is for a composite metric (i.e. one that is composed of multiple metrics).
        /// </summary>
        [JsonIgnore]
        public bool IsCompositeMetric
        {
            get { return this.MetricIdentifiers != null && this.MetricIdentifiers.Count > 0; }
        }

        /// <summary>
        /// Gets or sets whether this event should be published by the metrics extension to any of the account defined endpoints.
        /// </summary>
        /// <remarks>
        /// If false, publication will not occur to the FE, but it will still be produced as a local aggregate on the client machine for Cosmos upload.
        /// </remarks>
        [JsonProperty("pe", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(true)]
        public bool EnableClientSidePublication { get; set; }

        /// <summary>
        /// Gets or sets whether this event should be published by the metrics extension to any of the external account endpoints.
        /// </summary>
        /// <remarks>
        /// If false, publication will still occur to the MDM primary endpoint but not to any of the external/forking endpoints.
        /// </remarks>
        [JsonProperty("fe", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(true)]
        public bool EnableClientSideForking { get; set; }

        /// <summary>
        /// Gets or sets whether this event should be published to the aggregated ETW provider.
        /// </summary>
        [JsonProperty("epe", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(true)]
        public bool EnableClientSideEtwPublication { get; set; }

        /// <summary>
        /// Gets or sets ingestions options to be used for this metric.
        /// </summary>
        /// <remarks>
        /// If false, publication will still occur to the MDM primary endpoint but not to any of the external/forking endpoints.
        /// </remarks>
        [JsonProperty("igo", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(true)]
        public short IngestionOptions { get; set; }

        /// <summary>
        /// Gets or sets whether metrics in composite metrics should be considered as zero when metric is not reported or is missing.
        /// </summary>
        /// <remarks>
        /// Only used for Composite metrics.
        /// </remarks>
        [JsonProperty("tz", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool TreatMissingSeriesAsZeros { get; set; }

        /// <summary>
        /// Gets the default scaling factor if <paramref name="metric" /> contains scaling indicators.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metric">The metric name.</param>
        /// <returns>
        /// The default scaling factor.
        /// </returns>
        public static float? GetDefaultScalingFactor(string metricNamespace, string metric)
        {
            float? defaultScalingFactor = null;

            foreach (var entry in DefaultScalingFactorForMetrics)
            {
                if (metric.Contains(entry.Key))
                {
                    defaultScalingFactor = entry.Value;
                }
            }

            return defaultScalingFactor;
        }

        /// <summary>
        /// Determines whether an instance is equal to this instance.
        /// </summary>
        /// <param name="other">Instance to compare to this instance.</param>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        public bool Equals(MetricConfigurationV2 other)
        {
            if (other.MetricName == null)
            {
                if (this.MetricName == null)
                {
                    return true;
                }

                return false;
            }

            if (this.MetricName == null)
            {
                return false;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(this.MetricName, other.MetricName);
        }

        /// <summary>
        /// Gets hashcode.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(this.MetricName);
        }

        /// <summary>
        /// Overrides Object.Equals.
        /// </summary>
        /// <param name="obj">Object to compare with.</param>
        /// <returns>True if equals, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is MetricConfigurationV2))
            {
                return false;
            }

            return this.Equals((MetricConfigurationV2)obj);
        }
    }
}
