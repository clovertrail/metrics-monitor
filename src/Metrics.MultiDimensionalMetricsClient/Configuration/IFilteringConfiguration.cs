//-------------------------------------------------------------------------------------------------
// <copyright file="IFilteringConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// Determines if the filtering is enabled for this pre-aggregate based on configured pre-aggregate filters.
    /// </summary>
    public interface IFilteringConfiguration
    {
        /// <summary>
        /// Determines if the filtering is enabled for this pre-aggregate based on configured pre-aggregate filters.
        /// </summary>
        bool Enabled { get; }
    }
}