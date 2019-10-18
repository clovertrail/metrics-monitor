// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFilteredTimeSeries.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System.Collections.Generic;
    using Metrics;
    using Microsoft.Online.Metrics.Serialization.Configuration;

    /// <summary>
    /// An interface for filtered time series.
    /// </summary>
    public interface IFilteredTimeSeries
    {
        /// <summary>
        /// Gets the metric identifier.
        /// </summary>
        MetricIdentifier MetricIdentifier { get; }

        /// <summary>
        /// Set of valid dimension name-value pairs that meet the query condition.
        /// </summary>
        IReadOnlyList<KeyValuePair<string, string>> DimensionList { get; }

        /// <summary>
        /// Gets the evaluated value for this time series that meets the condition set in the query (provided for evidence and/or sorting).
        /// </summary>
        double EvaluatedResult { get; }

        /// <summary>
        /// Gets the full collection time series values for the query interval. It should be null if
        /// the query did not request the full collection of values to be returned.
        /// </summary>
        /// <remarks>double.NaN is the sentinel used to indicate there is no metric value.</remarks>
        IReadOnlyList<KeyValuePair<SamplingType, double[]>> TimeSeriesValues { get; }
    }
}