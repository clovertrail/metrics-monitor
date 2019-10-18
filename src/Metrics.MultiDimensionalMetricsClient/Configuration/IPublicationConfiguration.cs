//-------------------------------------------------------------------------------------------------
// <copyright file="IPublicationConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// Determine which metric storage should be used for the data from this preaggregate.
    /// </summary>
    public interface IPublicationConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether cache server publication is disabled.
        /// </summary>
        bool CacheServerPublicationDisabled { get; }

        /// <summary>
        /// Determines if the feature is enabled or disabled.
        /// </summary>
        bool MetricStorePublicationEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the preaggregate should be published as an aggregated metrics store metric.
        /// </summary>
        bool AggregatedMetricsStorePublication { get; }
    }
}