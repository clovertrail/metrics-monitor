// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IQueryResultV3.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System.Collections.Generic;

    using Microsoft.Cloud.Metrics.Client.Metrics;

    /// <summary>
    /// Represents a single time series result, with one or more sampling types.
    /// </summary>
    public interface IQueryResultV3
    {
        /// <summary>
        /// Set of valid dimension name-value pairs that meet the query condition.
        /// </summary>
        IReadOnlyList<KeyValuePair<string, string>> DimensionList { get; }

        /// <summary>
        /// Gets the evaluated value for this time series that meets the condition set in the query (provided for evidence and/or sorting).
        /// </summary>
        double EvaluatedResult { get; }

        /// <summary>
        /// Gets the time series values for the requested sampling type.
        /// </summary>
        /// <param name="samplingType">The sampling type requested.</param>
        /// <returns>The array of datapoints for the requested sampling type/</returns>
        /// <remarks>
        /// double.NaN is the sentinel used to indicate there is no metric value.
        /// </remarks>
        double[] GetTimeSeriesValues(SamplingType samplingType);
    }
}
