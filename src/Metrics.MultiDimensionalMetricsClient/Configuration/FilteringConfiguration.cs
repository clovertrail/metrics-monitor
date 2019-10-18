//-------------------------------------------------------------------------------------------------
// <copyright file="FilteringConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using Newtonsoft.Json;

    /// <summary>
    /// Determine if the filtering is enabled or disabled for this preaggregate.
    /// </summary>
    public sealed class FilteringConfiguration : IFilteringConfiguration
    {
        /// <summary>
        /// IFilteringConfiguration where filtering is enabled.
        /// </summary>
        public static readonly IFilteringConfiguration FilteringEnabled = new FilteringConfiguration(true);

        /// <summary>
        /// IFilteringConfiguration where filtering is disabled.
        /// </summary>
        public static readonly IFilteringConfiguration FilteringDisabled = new FilteringConfiguration(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteringConfiguration"/> class.
        /// </summary>
        /// <param name="enabled">Whether or not the feature is enabled.</param>
        [JsonConstructor]
        internal FilteringConfiguration(bool enabled)
        {
            this.Enabled = enabled;
        }

        /// <summary>
        /// Determines if the filtering is enabled for this pre-aggregate based on configured pre-aggregate filters.
        /// </summary>
        public bool Enabled { get; }
    }
}
