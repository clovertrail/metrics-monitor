//-------------------------------------------------------------------------------------------------
// <copyright file="MonitorHealthStatus.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client
{
    using System;

    using Microsoft.Online.Metrics.Serialization.Monitor;

    /// <summary>
    /// The class representing the monitor status.
    /// </summary>
    internal sealed class MonitorHealthStatus : IMonitorHealthStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorHealthStatus"/> class.
        /// </summary>
        /// <param name="healthy">if set to <c>true</c> [healthy].</param>
        /// <param name="timeStamp">The time stamp of the last monitor report.</param>
        /// <param name="message">The message for the last monitor report.</param>
        public MonitorHealthStatus(bool healthy, DateTimeOffset? timeStamp, string message)
        {
            this.Healthy = healthy;
            this.TimeStamp = timeStamp;
            this.Message = message;
        }

        /// <summary>
        /// Gets a value indicating the healthy status of the last report.
        /// </summary>
        public bool Healthy { get; private set; }

        /// <summary>
        /// Gets the last time when monitor reported a status
        /// </summary>
        public DateTimeOffset? TimeStamp { get; private set; }

        /// <summary>
        /// Gets the message for the last monitor report
        /// </summary>
        public string Message { get; private set; }
    }
}