// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEtlSubscriber.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Defines the subscribers of ETL files being periodically cut and process each one of them.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;

    /// <summary>
    /// Enumeration with the possible status at the end of the processing of a single ETL file.
    /// </summary>
    internal enum EtlCompletionStatus
    {
        /// <summary>
        /// ETL file was successfully processed.
        /// </summary>
        Success,

        /// <summary>
        /// Cancellation was requested while processing the ETL file.
        /// </summary>
        CancellationRequested,

        /// <summary>
        /// The ETL file could not being opened.
        /// </summary>
        FailureToOpen,

        /// <summary>
        /// An exception happened while processing the ETL file.
        /// </summary>
        ErrorWhileProcessing,
    }

    /// <summary>
    /// Defines the subscribers of ETL files being periodically cut and process each one of them.
    /// </summary>
    /// <remarks>
    /// The subscriber is going to receive the raw callbacks provided by ETW to process ETW sessions
    /// (RecordCallback and BufferCallback).
    /// </remarks>
    internal unsafe interface IEtlSubscriber
    {
        /// <summary>
        /// Gets the configuration needed to processes backlog of ETL files.
        /// </summary>
        EtlBacklogConfig EtlBacklogConfig { get; }

        /// <summary>
        /// Gets the minimum interval that the dispatcher should wait between dispatching files to the
        /// subscriber.
        /// </summary>
        TimeSpan MinimumIntervalBetweenEtlFiles { get; }

        /// <summary>
        /// Called when the processing of an ETL file is about to start.
        /// </summary>
        /// <param name="etlFileName">
        /// Name of the ETL file that is about to be processed.
        /// </param>
        /// <remarks>
        /// The subscriber should be processing a single ETL file at time, calls to the ETW callbacks
        /// (see below) will only start after this method is called for the ETL file.
        /// </remarks>
        void Start(string etlFileName);

        /// <summary>
        /// Receives the ETW callback passing the ETW events from the ETL file currently being processed,
        /// see EventRecordCallback on MSDN for more information.
        /// </summary>
        /// <param name="eventRecord">
        /// Pointer to the event record received from the ETW session.
        /// </param>
        void RecordCallback(NativeMethods.EventRecord* eventRecord);

        /// <summary>
        /// Receives the ETW callback passing the ETW buffers from the ETL file currently being processed,
        /// see EventTraceBufferCallback on MSDN for more information.
        /// </summary>
        /// <param name="eventTraceLog">
        /// Pointer to the event trace log structure but due to the nature of this structure (variable size
        /// and containing null terminated strings) it is easier to perform marshaling in the code (if needed).
        /// </param>
        /// <returns>
        /// True if the processing of the trace should continue, false to stop processing the trace.
        /// </returns>
        bool BufferCallback(IntPtr eventTraceLog);

        /// <summary>
        /// Notifies that the processing for the given ETL file is complete and let the subscriber knows
        /// if the processed stop because cancellation or not.
        /// </summary>
        /// <param name="etlFileName">
        /// Name of the ETL file that finished being processed.
        /// </param>
        /// <param name="completionStatus">
        /// Enumeration that gives information about the completion status, i.e.: error, success, etc.
        /// </param>
        /// <param name="exception">
        /// In case of error during the processing this will have the exception that was captured.
        /// Use this in combination with the completion status to fully diagnostic what happened.
        /// </param>
        void End(string etlFileName, EtlCompletionStatus completionStatus, Exception exception);
    }
}