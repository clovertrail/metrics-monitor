//-------------------------------------------------------------------------------------------------
// <copyright file="IPercentileConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// Determine if the percentile sampling types are available for this preaggregate.
    /// </summary>
    public interface IPercentileConfiguration
    {
        /// <summary>
        /// Determines if the percentile sampling types are enabled or disabled.
        /// </summary>
        bool Enabled { get; }
    }
}