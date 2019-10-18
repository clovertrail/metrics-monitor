//-------------------------------------------------------------------------------------------------
// <copyright file="IQueryResult.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a single time series that was selected and is being returned as a query result.
    /// </summary>
    public interface IQueryResult
    {
        /// <summary>
        /// Set of valid dimension name-value pairs that meet the query condition.
        /// </summary>
        IReadOnlyList<KeyValuePair<string, string>> DimensionList { get; }

        /// <summary>
        /// Gets the evaluated value for this time series that meets the condition set in the query (provided for evidence and/or sorting).
        /// </summary>
        double? EvaluatedResult { get; }

        /// <summary>
        /// Gets the full collection time series values for the query interval. It should be null if
        /// the query did not request the full collection of values to be returned.
        /// </summary>
        double?[] TimeSeries { get; }
    }
}
