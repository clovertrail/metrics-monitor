// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IQueryResultListV3.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Holds the list of query results and the associated metadata.
    /// </summary>
    public interface IQueryResultListV3
    {
        /// <summary>
        /// Gets the end time in UTC for the query results.
        /// </summary>
        DateTime EndTimeUtc { get; }

        /// <summary>
        /// Gets the start time in UTC for the query results.
        /// </summary>
        DateTime StartTimeUtc { get; }

        /// <summary>
        /// Gets the time resolution in milliseconds for the query results.
        /// </summary>
        int TimeResolutionInMinutes { get; }

        /// <summary>
        /// Gets the query results. Each result represent a single time series where start time, end time and time resolution
        /// is represented by this object members.
        /// </summary>
        IReadOnlyList<IQueryResultV3> Results { get; }
    }
}
