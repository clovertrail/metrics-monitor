//-------------------------------------------------------------------------------------------------
// <copyright file="MinMaxConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using Newtonsoft.Json;

    /// <summary>
    /// Determine if the minimum and maximum sampling types are available for this preaggregate.
    /// </summary>
    public sealed class MinMaxConfiguration : IMinMaxConfiguration
    {
        /// <summary>
        /// MinMaxConfiguration where minimum and maximum sampling types are enabled.
        /// </summary>
        public static readonly MinMaxConfiguration MinMaxEnabled = new MinMaxConfiguration(true);

        /// <summary>
        /// MinMaxConfiguration where minimum and maximum sampling types are disabled.
        /// </summary>
        public static readonly MinMaxConfiguration MinMaxDisabled = new MinMaxConfiguration(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="MinMaxConfiguration"/> class.
        /// </summary>
        /// <param name="enabled">Whether or not the feature is enabled.</param>
        [JsonConstructor]
        internal MinMaxConfiguration(bool enabled)
        {
            this.Enabled = enabled;
        }

        /// <summary>
        /// Determines if the minimum and maximum sampling types are enabled or disabled.
        /// </summary>
        public bool Enabled { get; }
    }
}
