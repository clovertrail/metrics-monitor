// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueryResultListV3.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// Holds the list of query results and the associated metadata.
    /// </summary>
    /// <seealso cref="IQueryResultListV3" />
    public sealed class QueryResultListV3 : IQueryResultListV3
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryResultListV3"/> class.
        /// </summary>
        /// <param name="startTimeUtc">The start time.</param>
        /// <param name="endTimeUtc">The end time.</param>
        /// <param name="timeResolutionInMinutes">The time resolution in milliseconds.</param>
        /// <param name="results">The result time series.</param>
        [JsonConstructor]
        internal QueryResultListV3(DateTime startTimeUtc, DateTime endTimeUtc, int timeResolutionInMinutes, IReadOnlyList<IQueryResultV3> results)
        {
            this.StartTimeUtc = startTimeUtc;
            this.EndTimeUtc = endTimeUtc;
            this.TimeResolutionInMinutes = timeResolutionInMinutes;
            this.Results = results;
        }

        /// <summary>
        /// Gets the end time in UTC for the query results.
        /// </summary>
        public DateTime EndTimeUtc { get; }

        /// <summary>
        /// Gets the start time in UTC for the query results.
        /// </summary>
        public DateTime StartTimeUtc { get; }

        /// <summary>
        /// Gets the time resolution in milliseconds for the query results.
        /// </summary>
        public int TimeResolutionInMinutes { get; }

        /// <summary>
        /// Gets the query results. Each result represent a single time series where start time, end time and time resolution
        /// is represented by this object members.
        /// </summary>
        public IReadOnlyList<IQueryResultV3> Results { get; }
    }
}
