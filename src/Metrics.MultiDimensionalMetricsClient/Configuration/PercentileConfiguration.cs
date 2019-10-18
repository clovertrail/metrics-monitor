//-------------------------------------------------------------------------------------------------
// <copyright file="PercentileConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using Newtonsoft.Json;

    /// <summary>
    /// Determine if the percentile sampling types are available for this preaggregate.
    /// </summary>
    public sealed class PercentileConfiguration : IPercentileConfiguration
    {
        /// <summary>
        /// PercentileConfiguration where percentile sampling types are enabled.
        /// </summary>
        public static readonly PercentileConfiguration PercentileEnabled = new PercentileConfiguration(true);

        /// <summary>
        /// PercentileConfiguration where percentile sampling types are disabled.
        /// </summary>
        public static readonly PercentileConfiguration PercentileDisabled = new PercentileConfiguration(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="PercentileConfiguration"/> class.
        /// </summary>
        /// <param name="enabled">Whether or not the feature is enabled.</param>
        [JsonConstructor]
        internal PercentileConfiguration(bool enabled)
        {
            this.Enabled = enabled;
        }

        /// <summary>
        /// Determines if the percentile sampling types are enabled or disabled.
        /// </summary>
        public bool Enabled { get; }
    }
}
