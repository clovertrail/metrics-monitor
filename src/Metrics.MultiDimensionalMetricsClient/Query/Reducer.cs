// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Reducer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    /// <summary>
    /// Reducing function to use on time series data.
    /// </summary>
    public enum Reducer
    {
        /// <summary>
        /// Reducer not specified.  Query is not considered valid.
        /// </summary>
        Undefined,

        /// <summary>
        /// Not an reducer per se, it means that any value on the series should be evaluated by the <see cref="Operator"/>.
        /// </summary>
        Any,

        /// <summary>
        /// Not an reducer per se, it means that all values on the series should be evaluated by the <see cref="Operator"/>.
        /// The evaluation is relaxed in the sense that missing data points (null) are going to be considered
        /// as satisfying the <see cref="Operator"/> condition, but at least one data point should exist satisfying the <see cref="Operator"/>.
        /// </summary>
        All,

        /// <summary>
        /// Not an reducer per se, it means that all values on the series should be evaluated by the <see cref="Operator"/>.
        /// The evaluation is strict in the sense that missing data points (null) are going to be considered
        /// as not satisfying the <see cref="Operator"/> conditions.
        /// </summary>
        AllStrict,

        /// <summary>
        /// Reduce by calculating the average of all values in the series. Only data points different than
        /// null are considered for the average calculation.
        /// </summary>
        Average,

        /// <summary>
        /// Reduce by adding all values in the series.
        /// </summary>
        Sum,

        /// <summary>
        /// Reduce by selecting the minimum value of the series.
        /// </summary>
        Min,

        /// <summary>
        /// Reduce by selecting the maximum value of the series.
        /// </summary>
        Max
    }
}
