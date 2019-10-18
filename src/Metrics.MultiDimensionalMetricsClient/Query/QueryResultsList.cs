//-------------------------------------------------------------------------------------------------
// <copyright file="QueryResultsList.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Utility;

    /// <summary>
    /// Represents a list of IQueryResult
    /// </summary>
    public sealed class QueryResultsList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryResultsList"/> class.
        /// </summary>
        /// <param name="startTimeUtc">The start time.</param>
        /// <param name="endTimeUtc">The end time.</param>
        /// <param name="timeResolutionInMilliseconds">The time resolution in milliseconds.</param>
        /// <param name="results">The results.</param>
        [JsonConstructor]
        internal QueryResultsList(long startTimeUtc, long endTimeUtc, long timeResolutionInMilliseconds, IReadOnlyList<QueryResult> results)
        {
            this.StartTimeUtc = UnixEpochHelper.FromMillis(startTimeUtc);
            this.EndTimeUtc = UnixEpochHelper.FromMillis(endTimeUtc);
            this.TimeResolutionInMilliseconds = timeResolutionInMilliseconds;
            this.Results = results;
        }

        /// <summary>
        /// Gets the end time in UTC for the query results.
        /// </summary>
        public DateTime EndTimeUtc { get; private set; }

        /// <summary>
        /// Gets the start time in UTC for the query results.
        /// </summary>
        public DateTime StartTimeUtc { get; private set; }

        /// <summary>
        /// Gets the time resolution in milliseconds for the query results.
        /// </summary>
        public long TimeResolutionInMilliseconds { get; private set; }

        /// <summary>
        /// Gets the query results. Each result represent a single time series where start time, end time and time resolution
        /// is represented by this object members.
        /// </summary>
        public IReadOnlyList<IQueryResult> Results { get; private set; }
    }
}
