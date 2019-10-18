//-------------------------------------------------------------------------------------------------
// <copyright file="PublicationConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using Newtonsoft.Json;

    /// <summary>
    /// Determine which metric storage should be used for the data from this preaggregate.
    /// </summary>
    public sealed class PublicationConfiguration : IPublicationConfiguration
    {
        /// <summary>
        /// Data store configuration where data will publish to metric store only.
        /// </summary>
        public static readonly PublicationConfiguration MetricStore = new PublicationConfiguration(true, true, false);

        /// <summary>
        /// Data store configuration where data will publish to cache server only.
        /// </summary>
        public static readonly PublicationConfiguration CacheServer = new PublicationConfiguration(false, false, false);

        /// <summary>
        /// Data store configuration where data will publish to cache server and metric store.
        /// </summary>
        public static readonly PublicationConfiguration CacheServerAndRawMetricsStore = new PublicationConfiguration(true, false, false);

        /// <summary>
        /// Data store configuration where data will publish to cache server and metric store as an aggregated metric.
        /// </summary>
        public static readonly PublicationConfiguration CacheServerAndAggregatedMetricsStore = new PublicationConfiguration(true, false, true);

        /// <summary>
        /// Data store configuration where data will publish to metrics store as an aggregated metric only.
        /// </summary>
        public static readonly PublicationConfiguration AggregatedMetricsStore = new PublicationConfiguration(true, true, true);

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicationConfiguration" /> class.
        /// </summary>
        /// <param name="metricStorePublicationEnabled">If metric store publication is enabled.</param>
        /// <param name="cacheServerPublicationDisabled">IF cache server publication is disabled.</param>
        /// <param name="aggregatedMetricsStorePublication">If aggregated metrics store publication is enabled.</param>
        [JsonConstructor]
        internal PublicationConfiguration(bool metricStorePublicationEnabled, bool cacheServerPublicationDisabled, bool aggregatedMetricsStorePublication)
        {
            this.MetricStorePublicationEnabled = metricStorePublicationEnabled;
            this.CacheServerPublicationDisabled = cacheServerPublicationDisabled;
            this.AggregatedMetricsStorePublication = aggregatedMetricsStorePublication;
        }

        /// <summary>
        /// Determines if the metrics store is enabled or disabled.
        /// </summary>
        public bool MetricStorePublicationEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether cache server publication is disabled.
        /// </summary>
        public bool CacheServerPublicationDisabled { get; }

        /// <summary>
        /// Gets a value indicating whether the preaggregate should be published as an aggregated metrics store metric.
        /// </summary>
        public bool AggregatedMetricsStorePublication { get; }
    }
}