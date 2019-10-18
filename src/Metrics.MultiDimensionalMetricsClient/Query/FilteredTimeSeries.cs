//-------------------------------------------------------------------------------------------------
// <copyright file="FilteredTimeSeries.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System.Collections.Generic;
    using System.Text;
    using Metrics;

    using Microsoft.Online.Metrics.Serialization.Configuration;

    /// <summary>
    /// Represents a single time series that was selected and is being returned as an item in <see cref="FilteredTimeSeriesQueryResponse"/>.
    /// </summary>
    public sealed class FilteredTimeSeries : IFilteredTimeSeries, IQueryResultV3
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredTimeSeries" /> class.
        /// Create a single time series.
        /// </summary>
        /// <param name="metricIdentifier">The metric identifier.</param>
        /// <param name="dimensionList">The list of dimensions.</param>
        /// <param name="evaluatedResult">The evaluated result.</param>
        /// <param name="seriesValues">The time series values, only included if specified in the query.</param>
        internal FilteredTimeSeries(
            MetricIdentifier metricIdentifier,
            IReadOnlyList<KeyValuePair<string, string>> dimensionList,
            double evaluatedResult,
            IReadOnlyList<KeyValuePair<SamplingType, double[]>> seriesValues)
        {
            this.MetricIdentifier = metricIdentifier;
            this.DimensionList = dimensionList;
            this.EvaluatedResult = evaluatedResult;
            this.TimeSeriesValues = seriesValues;
        }

        /// <summary>
        /// Gets the metric identifier.
        /// </summary>
        public MetricIdentifier MetricIdentifier { get; }

        /// <summary>
        /// Set of valid dimension name-value pairs that meet the query condition.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, string>> DimensionList { get; }

        /// <summary>
        /// Gets the evaluated value for this time series that meets the condition set in the query (provided for evidence and/or sorting).
        /// </summary>
        public double EvaluatedResult { get; }

        /// <summary>
        /// Gets the full collection time series values for the query interval. It should be null if
        /// the query did not request the full collection of values to be returned.
        /// </summary>
        public IReadOnlyList<KeyValuePair<SamplingType, double[]>> TimeSeriesValues { get; }

        /// <summary>
        /// Gets the time series values for the requested sampling type.
        /// </summary>
        /// <param name="samplingType">The sampling type requested.</param>
        /// <returns>The array of datapoints for the requested sampling type/</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown if the sampling type was not included in the response.</exception>
        /// <remarks>
        /// double.NaN is the sentinel used to indicate there is no metric value.
        /// </remarks>
        public double[] GetTimeSeriesValues(SamplingType samplingType)
        {
            for (var i = 0; i < this.TimeSeriesValues.Count; ++i)
            {
                if (samplingType.Equals(this.TimeSeriesValues[i].Key))
                {
                    return this.TimeSeriesValues[i].Value;
                }
            }

            throw new KeyNotFoundException($"Sampling type {samplingType} not found in the query result.");
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"EvaluatedResult: {this.EvaluatedResult}");
            sb.Append("Dimensions:");
            foreach (var pair in this.DimensionList)
            {
                sb.Append($"{pair.Key}: {pair.Value};");
            }

            sb.AppendLine();
            if (this.TimeSeriesValues != null && this.TimeSeriesValues.Count > 0)
            {
                sb.Append("[");
                for (int i = 0; i < this.TimeSeriesValues.Count; i++)
                {
                    sb.Append("[");
                    sb.Append($"{this.TimeSeriesValues[i].Key}, ");
                    sb.Append(string.Join(", ", this.TimeSeriesValues[i].Value));
                    sb.AppendLine("]");
                }

                sb.AppendLine("]");
            }

            return sb.ToString();
        }
    }
}
