// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEtlDispatcher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Defines the dispatchers of ETL files being periodically generated.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines the dispatchers of ETL files being periodically generated.
    /// </summary>
    /// <remarks>
    /// The dispatcher will receive the ETL files to be dispatched to the ETL file subscribers of
    /// a specific ETW collection.
    /// </remarks>
    internal interface IEtlDispatcher
    {
        /// <summary>
        /// Receives an list with all available ETL files that could be submitted to
        /// the subscriber. The dispatcher should expect this list to be sorted from
        /// oldest to newest available ETL.
        /// </summary>
        /// <param name="availableEtlFiles">
        /// Enumeration with all the available ETL files. The dispatcher willl do some filtering
        /// before submitting then to the subscriber.
        /// </param>
        void EnqueueBacklogEtls(List<string> availableEtlFiles);

        /// <summary>
        /// Enqueues an ETL file for processing.
        /// </summary>
        /// <param name="etlFileName">
        /// The ETL file to be enqueued for processing.
        /// </param>
        void EnqueueEtlFile(string etlFileName);
    }
}