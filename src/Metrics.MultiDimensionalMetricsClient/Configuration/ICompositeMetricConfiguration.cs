//-------------------------------------------------------------------------------------------------
// <copyright file="ICompositeMetricConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a composite metric in the MDM System.
    /// </summary>
    public interface ICompositeMetricConfiguration : IMetricConfiguration
    {
        /// <summary>
        /// Gets the metric sources.
        /// </summary>
        IEnumerable<CompositeMetricSource> MetricSources { get; }

        /// <summary>
        /// Gets the composite expressions.
        /// </summary>
        IEnumerable<CompositeExpression> CompositeExpressions { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to treat missing series as zeroes.
        /// </summary>
        bool TreatMissingSeriesAsZeroes { get; set; }

        /// <summary>
        /// Adds the metric source.
        /// </summary>
        /// <param name="metricSource">The metric source.</param>
        void AddMetricSource(CompositeMetricSource metricSource);

        /// <summary>
        /// Removes the metric source.
        /// </summary>
        /// <param name="metricSourceName">The metric source name.</param>
        void RemoveMetricSource(string metricSourceName);

        /// <summary>
        /// Adds the expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        void AddExpression(CompositeExpression expression);

        /// <summary>
        /// Removes the expression.
        /// </summary>
        /// <param name="expressionName">The expression name.</param>
        void RemoveExpression(string expressionName);
    }
}