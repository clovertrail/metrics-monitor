// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricEnrichmentRule.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.MetricEnrichmentRuleManagement
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a metric enrichment rule.
    /// </summary>
    /// <remarks>
    /// TODO: Add a metric enrichment rule builder once we are ready to announce this to customer.
    /// </remarks>
    public sealed class MetricEnrichmentRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricEnrichmentRule"/> class.
        /// </summary>
        /// <param name="stampId">The stamp identifier this rule belongs too.</param>
        /// <param name="monitoringAccountFilter">
        /// The monitoring account filter to be used for matching applicable rules for a given monitoring account.
        /// Use * to represent Wild card filter.
        /// </param>
        /// <param name="metricNamespaceFilter">
        /// The metric namespace filter to be used for matching applicable rules for a given metric namespace.
        /// Use * to represent Wild card filter.
        /// </param>
        /// <param name="metricNameFilter">
        /// The metric name filter to be used for matching applicable rules for a given metric name.
        /// Use * to represent Wild card filter.
        /// </param>
        /// <param name="transformations">Represents the transformations associated with this rule..</param>
        public MetricEnrichmentRule(string stampId, string monitoringAccountFilter, string metricNamespaceFilter, string metricNameFilter, List<MetricEnrichmentRuleTransformationDefinition> transformations)
        {
            if (string.IsNullOrEmpty(stampId))
            {
                throw new ArgumentNullException(nameof(stampId));
            }

            if (string.IsNullOrEmpty(monitoringAccountFilter))
            {
                throw new ArgumentNullException(nameof(monitoringAccountFilter));
            }

            if (string.IsNullOrEmpty(metricNamespaceFilter))
            {
                throw new ArgumentNullException(nameof(metricNamespaceFilter));
            }

            if (string.IsNullOrEmpty(metricNameFilter))
            {
                throw new ArgumentNullException(nameof(metricNameFilter));
            }

            this.StampId = stampId;
            this.MonitoringAccountFilter = monitoringAccountFilter;
            this.MetricNamespaceFilter = metricNamespaceFilter;
            this.MetricNameFilter = metricNameFilter;
            this.Transformations = transformations;
        }

        /// <summary>
        /// Gets the stamp identifier this rule belongs too.
        /// </summary>
        public string StampId { get; private set; }

        /// <summary>
        /// Gets the filter to be used for matching applicable rules for a given monitoring account.
        /// </summary>
        /// <remarks>
        /// Use * to represent Wild card filter.
        /// </remarks>
        public string MonitoringAccountFilter { get; private set; }

        /// <summary>
        /// Gets the filter to be used for matching applicable rules for a given metric namespace.
        /// </summary>
        /// <remarks>
        /// Use * to represent Wild card filter.
        /// </remarks>
        public string MetricNamespaceFilter { get; private set; }

        /// <summary>
        /// Gets the filter to be used for matching applicable rules for a given metric name.
        /// </summary>
        /// <remarks>
        /// Use * to represent Wild card filter.
        /// </remarks>
        public string MetricNameFilter { get; private set; }

        /// <summary>
        /// Represents the transformations associated with this rule.
        /// </summary>
        public List<MetricEnrichmentRuleTransformationDefinition> Transformations { get; private set; }

        /// <summary>
        /// Validates the data is valid rule.
        /// </summary>
        /// <returns>
        /// Validation failure message, empty means validation passed
        /// </returns>
        internal string Validate()
        {
            if (string.IsNullOrEmpty(this.StampId))
            {
                return "Stamp id cannot be null";
            }

            if (string.IsNullOrEmpty(this.MonitoringAccountFilter))
            {
                return "MonitoringAccountFilter cannot be null";
            }

            if (string.IsNullOrEmpty(this.MetricNamespaceFilter))
            {
                return "MetricNamespaceFilter cannot be null";
            }

            if (string.IsNullOrEmpty(this.MetricNameFilter))
            {
                return "MetricNameFilter cannot be null";
            }

            if (this.Transformations == null || this.Transformations.Count == 0)
            {
                return "Transformations cannot be null or empty";
            }

            foreach (var transformation in this.Transformations)
            {
                var validationFailureMessage = transformation.Validate();
                if (!string.IsNullOrEmpty(validationFailureMessage))
                {
                    return validationFailureMessage;
                }
            }

            return string.Empty;
        }
    }
}
