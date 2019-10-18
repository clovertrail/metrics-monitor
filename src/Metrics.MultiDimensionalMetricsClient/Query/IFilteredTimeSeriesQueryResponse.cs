// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFilteredTimeSeriesQueryResponse.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An interface for filtered time series query response.
    /// </summary>
    public interface IFilteredTimeSeriesQueryResponse
    {
        /// <summary>
        /// Gets the query request.
        /// </summary>
        FilteredTimeSeriesQueryRequest QueryRequest { get; }

        /// <summary>
        /// Gets the start time in UTC for the query results.
        /// </summary>
        DateTime StartTimeUtc { get; }

        /// <summary>
        /// Gets the end time in UTC for the query results.
        /// </summary>
        DateTime EndTimeUtc { get; }

        /// <summary>
        /// Gets the time resolution in milliseconds for the query results.
        /// </summary>
        int TimeResolutionInMinutes { get; }

        /// <summary>
        /// Gets the <see cref="FilteredTimeSeries"/> list. Each item represents a single time series where start time, end time and time resolution
        /// is represented by this object members.
        /// </summary>
        IReadOnlyList<IFilteredTimeSeries> FilteredTimeSeriesList { get; }

        /// <summary>
        /// Gets the error code.
        /// </summary>
        FilteredTimeSeriesQueryResponseErrorCode ErrorCode { get; }

        /// <summary>
        /// Gets the diagnostics information.
        /// </summary>
        DiagnosticInfo DiagnosticInfo { get; }
    }
}