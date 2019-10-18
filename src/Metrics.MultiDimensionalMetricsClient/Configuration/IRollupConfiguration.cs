//-------------------------------------------------------------------------------------------------
// <copyright file="IRollupConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// Determine if rollup is enabled for this preaggregate.
    /// </summary>
    public interface IRollupConfiguration
    {
        /// <summary>
        /// Determines if the rollup is enabled or disabled.
        /// </summary>
        bool Enabled { get; }
    }
}