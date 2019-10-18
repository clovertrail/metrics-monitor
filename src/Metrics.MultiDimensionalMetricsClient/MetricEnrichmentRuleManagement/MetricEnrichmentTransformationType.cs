// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricEnrichmentTransformationType.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.MetricEnrichmentRuleManagement
{
    /// <summary>
    /// Represents the metric enrichment transformation type.
    /// </summary>
    public enum MetricEnrichmentTransformationType
    {
        /// <summary>
        /// Adds new dimensions to the event.
        /// </summary>
        Add,

        /// <summary>
        /// Replaces existing dimension in the event.
        /// </summary>
        Replace,

        /// <summary>
        /// Drops the event.
        /// </summary>
        Drop
    }
}
