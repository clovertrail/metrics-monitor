//-------------------------------------------------------------------------------------------------
// <copyright file="MetricNamespaceConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.Configuration
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The data structure that the batch API for fetching metric configuration returns per namespace
    /// in the new (Monitoring Agent) pipeline. This is JSON serialized to the clients.
    /// </summary>
    public sealed class MetricNamespaceConfiguration
    {
        private DateTime updatedAtUtc;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricNamespaceConfiguration"/> class.
        /// </summary>
        public MetricNamespaceConfiguration()
        {
            this.MetricConfigurations = new HashSet<MetricConfigurationV2>();
        }

        /// <summary>
        /// Gets or sets the metric namespace.
        /// </summary>
        [JsonProperty(PropertyName = "m")]
        public string MetricNamespace { get; set; }

        /// <summary>
        /// Gets or sets the timestamp in UTC of the last updated metric in this namespace.
        /// </summary>
        [JsonProperty(PropertyName = "u")]
        public DateTime UpdatedAtUtc
        {
            get
            {
                return this.updatedAtUtc;
            }

            set
            {
                // Metrics extension today ignores sub-seconds, so we need to work around it for now.
                // We also always advance this property with the recent configuration cache refresh time
                // to avoid missing configurations, even if no configuration was updated at all.
                this.updatedAtUtc = TruncateToIntegralSeconds(value);
            }
        }

        /// <summary>
        /// Gets or sets the array of metric configurations.
        /// </summary>
        [JsonProperty(PropertyName = "c")]
        public HashSet<MetricConfigurationV2> MetricConfigurations { get; set; }

        /// <summary>
        /// Truncates to integral seconds.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>
        /// The trucated date time.
        /// </returns>
        private static DateTime TruncateToIntegralSeconds(DateTime dateTime)
        {
            return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
        }
    }
}
