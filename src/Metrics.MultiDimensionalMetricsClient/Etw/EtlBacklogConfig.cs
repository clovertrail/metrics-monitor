// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EtlBacklogConfig.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Type that contains the information describing how backlog of ETL files should be processed.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;

    /// <summary>
    /// Type that contains the information describing how backlog of ETL files should be processed.
    /// </summary>
    internal struct EtlBacklogConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EtlBacklogConfig"/> struct.
        /// </summary>
        /// <param name="shouldReceiveBacklogFiles">
        /// A boolean indicating whether the subscriber should receive any backlog of ETL files.
        /// </param>
        /// <param name="backlogTargetStartTime">
        /// The point in time from which the subscriber wants to receive backlog files.
        /// </param>
        /// <param name="maxBacklogFilesRequested">
        /// The maximum number of backlog files that are going to be passed to the subscriber.
        /// </param>
        public EtlBacklogConfig(bool shouldReceiveBacklogFiles, DateTime backlogTargetStartTime, int maxBacklogFilesRequested)
            : this()
        {
            this.ShouldReceiveBacklogFiles = shouldReceiveBacklogFiles;
            this.TargetStartTimeUtc = backlogTargetStartTime;
            this.MaxFiles = maxBacklogFilesRequested;
        }

        /// <summary>
        /// Gets a value indicating whether the subscriber should receive any backlog of ETL files.
        /// </summary>
        public bool ShouldReceiveBacklogFiles { get; private set; }

        /// <summary>
        /// Gets the point in time from which the subscriber wants to receive backlog files.
        /// </summary>
        /// <remarks>
        /// Since this point in time is likely contained in the middle of an ETL file and the
        /// dispatchers do not drop events prior to this time on behalf of the subscriber, the
        /// subscriber itself should be prepared to receive and ignore events prior to this point
        /// in time.
        /// </remarks>
        /// <remarks>
        /// If the number of backlog files to be processed from this point in time exceeds the
        /// <see cref="MaxFiles"/> the latter has priority and only that number of backlog files
        /// are going to be sent to the subscriber.
        /// </remarks>
        public DateTime TargetStartTimeUtc { get; private set; }

        /// <summary>
        /// Gets the maximum number of backlog files that are going to be passed to the subscriber.
        /// This is one way to put some kind of upper bound on the amount of work triggered by backlog
        /// files.
        /// </summary>
        /// <remarks>
        /// Notice how this value relates to the <see cref="TargetStartTimeUtc"/>.
        /// </remarks>
        public int MaxFiles { get; private set; }
    }
}