// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricEnrichmentRuleTransformationDefinition.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.MetricEnrichmentRuleManagement
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a tranformation definition to be applied to the metric event during metric data enrichment.
    /// </summary>
    public sealed class MetricEnrichmentRuleTransformationDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricEnrichmentRuleTransformationDefinition"/> class.
        /// </summary>
        /// <param name="executorId">The executor id to be used for transformation. Example: IdMapping based Addition Executor.</param>
        /// <param name="sourceEventDimensionNamesForKey">The source event dimension names the dimension names whose value will be used for building the key for lookup table in external data service.</param>
        /// <param name="executorConnectionStringOverride">The executor connection string override for executor connection string.</param>
        /// <param name="transformationType">The type of transformation the rule represents..</param>
        /// <param name="destinationColumnNamesForDimensions">
        /// The map where Key represents destination column to be used to extract dimension values
        /// and Value represents the final dimension to be used for the destination column value.
        /// </param>
        public MetricEnrichmentRuleTransformationDefinition(
            string executorId,
            List<string> sourceEventDimensionNamesForKey,
            string executorConnectionStringOverride,
            MetricEnrichmentTransformationType transformationType,
            Dictionary<string, string> destinationColumnNamesForDimensions)
        {
            this.ExecutorId = executorId;
            this.SourceEventDimensionNamesForKey = sourceEventDimensionNamesForKey;
            this.ExecutorConnectionStringOverride = executorConnectionStringOverride;
            this.TransformationType = transformationType;
            this.DestinationColumnNamesForDimensions = destinationColumnNamesForDimensions;
        }

        /// <summary>
        /// Gets or sets the executor id to be used for transformation. Example: IdMapping based Addition Executor.
        /// </summary>
        public string ExecutorId { get; }

        /// <summary>
        /// Gets or sets the dimension names whose value will be used for building the key for lookup table in external data service.
        /// </summary>
        public List<string> SourceEventDimensionNamesForKey { get; }

        /// <summary>
        /// Gets or sets the override for executor connection string.
        /// </summary>
        public string ExecutorConnectionStringOverride { get; }

        /// <summary>
        /// Represents the type of transformation the rule represents.
        /// </summary>
        public MetricEnrichmentTransformationType TransformationType { get; }

        /// <summary>
        /// Gets or sets the map
        /// where Key represents destination column to be used to extract dimension values
        /// and Value represents the final dimension to be used for the destination column value.
        /// </summary>
        public Dictionary<string, string> DestinationColumnNamesForDimensions { get; }

        /// <summary>
        /// Validates the data is valid rule.
        /// </summary>
        /// <returns>
        /// Validation failure message, empty means validation passed.
        /// </returns>
        internal string Validate()
        {
            if (string.IsNullOrEmpty(this.ExecutorId))
            {
                // TODO: Once executor's are finalized add executor id validation too.
                return "Executor id cannot be null";
            }

            return string.Empty;
        }
    }
}
