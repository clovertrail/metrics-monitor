//-------------------------------------------------------------------------------------------------
// <copyright file="QueryResult.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System.Collections.Generic;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a single time series that was selected and is being returned as a query result.
    /// </summary>
    internal sealed class QueryResult : IQueryResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryResult"/> class.
        /// Create a single query result
        /// </summary>
        /// <param name="dimensionList">The list of dimensions.</param>
        /// <param name="evaluatedResult">The evaluated result.</param>
        /// <param name="seriesValues">The time series values, only included if specified in the query.</param>
        [JsonConstructor]
        internal QueryResult(
            KeyValuePair<string, string>[] dimensionList,
            double? evaluatedResult,
            double?[] seriesValues)
        {
            this.DimensionList = dimensionList;
            this.EvaluatedResult = evaluatedResult;
            this.TimeSeries = seriesValues;
        }

        /// <summary>
        /// Set of valid dimension name-value pairs that meet the query condition.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, string>> DimensionList { get; private set; }

        /// <summary>
        /// Gets the evaluated value for this time series that meets the condition set in the query (provided for evidence and/or sorting).
        /// </summary>
        public double? EvaluatedResult { get; private set; }

        /// <summary>
        /// Gets the full collection time series values for the query interval. It should be null if
        /// the query did not request the full collection of values to be returned.
        /// </summary>
        public double?[] TimeSeries { get; private set; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("Result: {0}", this.EvaluatedResult));
            sb.Append("Dimensions:");
            foreach (var pair in this.DimensionList)
            {
                sb.Append(string.Format("{0}: {1};", pair.Key, pair.Value));
            }

            sb.AppendLine();
            if (this.TimeSeries != null && this.TimeSeries.Length > 0)
            {
                sb.Append("[");
                sb.Append(string.Join(", ", this.TimeSeries));
                sb.AppendLine("]");
            }

            return sb.ToString();
        }
    }
}
