//-------------------------------------------------------------------------------------------------
// <copyright file="IMinMaxConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// Determine if the minimum and maximum sampling types are available for this preaggregate.
    /// </summary>
    public interface IMinMaxConfiguration
    {
        /// <summary>
        /// Determines if the minimum and maximum sampling types are enabled or disabled.
        /// </summary>
        bool Enabled { get; }
    }
}