//-------------------------------------------------------------------------------------------------
// <copyright file="RollupConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using Newtonsoft.Json;

    /// <summary>
    /// Determine if rollup is enabled for this preaggregate.
    /// </summary>
    public sealed class RollupConfiguration : IRollupConfiguration
    {
        /// <summary>
        /// RollupConfiguration where rollup is enabled.
        /// </summary>
        public static readonly RollupConfiguration RollupEnabled = new RollupConfiguration(true);

        /// <summary>
        /// RollupConfiguration where rollup is disabled.
        /// </summary>
        public static readonly RollupConfiguration RollupDisabled = new RollupConfiguration(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="RollupConfiguration"/> class.
        /// </summary>
        /// <param name="enabled">Whether or not the feature is enabled.</param>
        [JsonConstructor]
        internal RollupConfiguration(bool enabled)
        {
            this.Enabled = enabled;
        }

        /// <summary>
        /// Determines if the rollup is enabled or disabled.
        /// </summary>
        public bool Enabled { get; }
    }
}
