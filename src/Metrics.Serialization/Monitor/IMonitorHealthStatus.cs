//-------------------------------------------------------------------------------------------------
// <copyright file="IMonitorHealthStatus.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.Monitor
{
    using System;

    /// <summary>
    /// The interface representing the monitor health status.
    /// </summary>
    public interface IMonitorHealthStatus
    {
        /// <summary>
        /// Gets a value indicating the healthy status of the last report.
        /// </summary>
        bool Healthy { get; }

        /// <summary>
        /// Gets the last time when monitor reported a status
        /// </summary>
        DateTimeOffset? TimeStamp { get; }

        /// <summary>
        /// Gets the message for the last monitor report
        /// </summary>
        string Message { get; }
    }
}