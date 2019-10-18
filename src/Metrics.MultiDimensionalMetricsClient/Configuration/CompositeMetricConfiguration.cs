//-------------------------------------------------------------------------------------------------
// <copyright file="CompositeMetricConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Online.Metrics.Serialization;

    /// <summary>
    /// Represents a composite metric in the MDM System.
    /// </summary>
    public sealed class CompositeMetricConfiguration : ICompositeMetricConfiguration
    {
        private readonly List<CompositeMetricSource> metricSources;
        private readonly List<CompositeExpression> compositeExpressions;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeMetricConfiguration"/> class.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="name">The name.</param>
        /// <param name="lastUpdatedTime">The last updated time.</param>
        /// <param name="lastUpdatedBy">The last updated by.</param>
        /// <param name="version">The version.</param>
        /// <param name="treatMissingSeriesAsZeroes">if set to <c>true</c> treat missing series as zeroes.</param>
        /// <param name="description">The description of the metric.</param>
        /// <param name="metricSources">The metric sources.</param>
        /// <param name="compositeExpressions">The composite expressions.</param>
        [JsonConstructor]
        internal CompositeMetricConfiguration(
            string metricNamespace,
            string name,
            DateTime lastUpdatedTime,
            string lastUpdatedBy,
            uint version,
            bool treatMissingSeriesAsZeroes,
            string description,
            IEnumerable<CompositeMetricSource> metricSources,
            IEnumerable<CompositeExpression> compositeExpressions)
        {
            this.MetricNamespace = metricNamespace;
            this.Name = name;
            this.LastUpdatedTime = lastUpdatedTime;
            this.LastUpdatedBy = lastUpdatedBy;
            this.Version = version;
            this.TreatMissingSeriesAsZeroes = treatMissingSeriesAsZeroes;
            this.Description = description;
            this.metricSources = metricSources.ToList();
            this.compositeExpressions = compositeExpressions.ToList();
        }

        /// <summary>
        /// The namespace of the metric.
        /// </summary>
        public string MetricNamespace { get; }

        /// <summary>
        /// The name of the metric.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The last updated time of the metric.
        /// </summary>
        public DateTime LastUpdatedTime { get; }

        /// <summary>
        /// The last entity to update the metric.
        /// </summary>
        public string LastUpdatedBy { get; }

        /// <summary>
        /// The version of the metric.
        /// </summary>
        public uint Version { get; }

        /// <summary>
        /// Gets the description of the metric.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the metric sources.
        /// </summary>
        public IEnumerable<CompositeMetricSource> MetricSources
        {
            get { return this.metricSources; }
        }

        /// <summary>
        /// Gets the composite expressions.
        /// </summary>
        public IEnumerable<CompositeExpression> CompositeExpressions
        {
            get { return this.compositeExpressions; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to treat missing series as zeroes.
        /// </summary>
        public bool TreatMissingSeriesAsZeroes { get; set; }

        /// <summary>
        /// Creates the composite metric configuration.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metric">The metric.</param>
        /// <param name="metricSources">The metric sources.</param>
        /// <param name="expressions">The expressions.</param>
        /// <param name="treatMissingSeriesAsZeroes">if set to <c>true</c> [treat missing series as zeroes].</param>
        /// <param name="description">The optional description of the metric.</param>
        /// <returns>The composite metric configuration.</returns>
        public static CompositeMetricConfiguration CreateCompositeMetricConfiguration(
            string metricNamespace,
            string metric,
            IEnumerable<CompositeMetricSource> metricSources,
            IEnumerable<CompositeExpression> expressions,
            bool treatMissingSeriesAsZeroes,
            string description = "")
        {
            if (string.IsNullOrWhiteSpace(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            if (string.IsNullOrWhiteSpace(metric))
            {
                throw new ArgumentNullException(nameof(metric));
            }

            if (metricSources == null)
            {
                throw new ArgumentNullException(nameof(metricSources));
            }

            if (expressions == null)
            {
                throw new ArgumentNullException(nameof(expressions));
            }

            if (description.Length > SerializationConstants.MaximumMetricDescriptionLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(description),
                    $"The metric description cannot be greater than {SerializationConstants.MaximumMetricDescriptionLength} characters.");
            }

            return new CompositeMetricConfiguration(metricNamespace, metric, DateTime.MinValue, string.Empty, 0, treatMissingSeriesAsZeroes, description, metricSources, expressions);
        }

        /// <summary>
        /// Adds the metric source.
        /// </summary>
        /// <param name="metricSource">The metric source.</param>
        public void AddMetricSource(CompositeMetricSource metricSource)
        {
            if (metricSource == null)
            {
                throw new ArgumentNullException(nameof(metricSource));
            }

            if (this.metricSources.Any(x => string.Equals(x.DisplayName, metricSource.DisplayName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConfigurationValidationException("Cannot add metric sources with duplicate names.", ValidationType.DuplicateMetricSource);
            }

            this.metricSources.Add(metricSource);
        }

        /// <summary>
        /// Removes the metric source.
        /// </summary>
        /// <param name="metricSourceName">The metric source name.</param>
        public void RemoveMetricSource(string metricSourceName)
        {
            this.metricSources.RemoveAll(x => string.Equals(x.DisplayName, metricSourceName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds the expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public void AddExpression(CompositeExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (this.compositeExpressions.Any(x => string.Equals(x.Name, expression.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConfigurationValidationException("Cannot add composite expressions with duplicate names.", ValidationType.DuplicateSamplingType);
            }

            this.compositeExpressions.Add(expression);
        }

        /// <summary>
        /// Removes the expression.
        /// </summary>
        /// <param name="expressionName">The expression name.</param>
        public void RemoveExpression(string expressionName)
        {
            this.compositeExpressions.RemoveAll(x => string.Equals(x.Name, expressionName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
