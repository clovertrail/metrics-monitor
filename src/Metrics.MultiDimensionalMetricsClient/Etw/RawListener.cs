// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RawListener.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Wraps the win32 calls to create an ETW listener that allows that client to receive the buffer and event callbacks.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Type that wraps the win32 calls to create an ETW listener that allows that client to receive
    /// the raw buffer and event callbacks when processing a trace (real-time or file).
    /// </summary>
    internal unsafe sealed class RawListener : IDisposable
    {
        /// <summary>
        /// The .Net 3.5 does not have Environment.Is64BitProcess use the sizeof(IntPtr) as a hack
        /// to figure this out.
        /// </summary>
        private static readonly bool Is64BitProcess = sizeof(IntPtr) == 8;

        /// <summary>
        /// Win32 opaque trace handles passed to the APIs to refer to the trace sessions being listened.
        /// </summary>
        /// <remarks>
        /// Create as an array because it is the way that it is passed to the OpenTrace API.
        /// </remarks>
        private readonly ulong[] traceHandles;

        /// <summary>
        /// Keeps the trace logs structures on memory while processing the traces.
        /// </summary>
        /// <remarks>
        /// Need to suppress ReSharper here because this struct ensures that the ETW callbacks
        /// are kept alive.
        /// </remarks>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly NativeMethods.EventTraceLogfilew[] traceLogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="RawListener"/> class.
        /// </summary>
        /// <param name="sessionOrEtlFiles">
        /// An enumerable with a single item if listen to an ETW real-time session or a sequence of ETL file names
        /// that are going to be processed together.
        /// </param>
        /// <param name="eventRecordCallback">
        /// The event record callback provided by the client. This can be null if the
        /// eventBufferCallback parameter is not.
        /// </param>
        /// <param name="eventBufferCallback">
        /// The event buffer callback provided by the client. This can be null if the
        /// eventRecordCallback parameter is not.
        /// </param>
        /// <param name="isFileTrace">
        /// Boolean to indicate whether this is a file or real-time ETW listener.
        /// </param>
        /// <param name="useRawTimestamps">
        /// True to indicate that the timestamps of events will be raw or false if ETW should convert them to FILETIME.
        /// </param>
        private RawListener(
            IEnumerable<string> sessionOrEtlFiles,
            NativeMethods.EventRecordCallback eventRecordCallback,
            NativeMethods.EventTraceBufferCallback eventBufferCallback,
            bool isFileTrace,
            bool useRawTimestamps = true)
        {
            if (sessionOrEtlFiles == null)
            {
                throw new ArgumentNullException("sessionOrEtlFiles");
            }

            var tracesEnum = from trace in sessionOrEtlFiles
                             where !string.IsNullOrEmpty(trace)
                             select trace;
            var traces = tracesEnum.ToList();
            if (traces.Count < 1)
            {
                throw new ArgumentException(
                    "At least one non-null, non-empty session or etl file name must be provided");
            }

            if (eventRecordCallback == null && eventBufferCallback == null)
            {
                throw new ArgumentException("At least one of the callbacks must be specified");
            }

            var traceIndex = 0;
            this.traceHandles = new ulong[traces.Count];
            this.traceLogs = new NativeMethods.EventTraceLogfilew[traces.Count];
            foreach (var traceName in traces)
            {
                this.traceLogs[traceIndex].EventCallback = eventRecordCallback;
                this.traceLogs[traceIndex].BufferCallback = eventBufferCallback;
                this.traceLogs[traceIndex].LogFileMode = NativeMethods.ProcessTraceModeEventRecord
                    | (useRawTimestamps ? NativeMethods.ProcessTraceModeRawTimestamp : 0);

                if (isFileTrace)
                {
                    this.traceLogs[traceIndex].LogFileName = traceName;
                }
                else
                {
                    this.traceLogs[traceIndex].LoggerName = traceName;
                    this.traceLogs[traceIndex].LogFileMode |= NativeMethods.ProcessTraceModeRealTime;
                }

                // New ETW session mode for Windows 8.1, Server 2012 R2, and later that avoids
                // slow consumers on one session to affect other sessions. Adding by default to all
                // sessions since it does not have adverse impact and it is just ignored by legacy OSes.
                this.traceLogs[traceIndex].LogFileMode |= NativeMethods.EventTraceIndependentSessionMode;

                var traceHandle = NativeMethods.OpenTrace(ref this.traceLogs[traceIndex]);
                if ((!Is64BitProcess && traceHandle == NativeMethods.InvalidTracehandle32) ||
                     (Is64BitProcess && traceHandle == NativeMethods.InvalidTracehandle64))
                {
                    throw new Win32Exception(string.Format(
                        CultureInfo.InvariantCulture,
                        "OpenTrace call for trace '{0}' failed.",
                        traceName));
                }

                this.traceHandles[traceIndex++] = traceHandle;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RawListener"/> class to listen to an
        /// ETW real-time session.
        /// </summary>
        /// <param name="sessionName">
        /// Name of the ETW real-time session from which the client wants to listen for events.
        /// </param>
        /// <param name="eventRecordCallback">
        /// The event record callback provided by the client. This can be null if the
        /// eventBufferCallback parameter is not.
        /// </param>
        /// <param name="eventBufferCallback">
        /// The event buffer callback provided by the client. This can be null if the
        /// eventRecordCallback parameter is not.
        /// </param>
        /// <param name="useRawTimestamps">
        /// True to indicate that the timestamps of events will be raw or false if ETW should convert them to FILETIME.
        /// </param>
        /// <returns>
        /// The new instance of the <see cref="RawListener"/> class to listen to an
        /// ETW real-time session.
        /// </returns>
        public static RawListener CreateRealTimeListener(
            string sessionName,
            NativeMethods.EventRecordCallback eventRecordCallback,
            NativeMethods.EventTraceBufferCallback eventBufferCallback,
            bool useRawTimestamps = true)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                throw new ArgumentException("Session name cannot be null or empty", "sessionName");
            }

            return new RawListener(new[] { sessionName }, eventRecordCallback, eventBufferCallback, false, useRawTimestamps);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RawListener"/> class to process a set of ETL files.
        /// </summary>
        /// <param name="etlFiles">
        /// Sequence with the names of the ETL files from which the client wants to listen for events.
        /// </param>
        /// <param name="eventRecordCallback">
        /// The event record callback provided by the client. This can be null if the
        /// eventBufferCallback parameter is not.
        /// </param>
        /// <param name="eventBufferCallback">
        /// The event buffer callback provided by the client. This can be null if the
        /// eventRecordCallback parameter is not.
        /// </param>
        /// <param name="useRawTimestamps">
        /// True to indicate that the timestamps of events will be raw or false if ETW should convert them to FILETIME.
        /// </param>
        /// <returns>
        /// The new instance of the <see cref="RawListener"/> class to process a set of ETL files.
        /// </returns>
        public static RawListener CreateEtlFileListener(
            IEnumerable<string> etlFiles,
            NativeMethods.EventRecordCallback eventRecordCallback,
            NativeMethods.EventTraceBufferCallback eventBufferCallback,
            bool useRawTimestamps = true)
        {
            if (etlFiles == null)
            {
                throw new ArgumentNullException("etlFiles");
            }

            var nonEmptyEtlsEnum = from etl in etlFiles
                             where !string.IsNullOrEmpty(etl)
                             select etl;
            var nonEmptyEtls = nonEmptyEtlsEnum.ToList();
            if (nonEmptyEtls.Count < 1)
            {
                throw new ArgumentException(
                    "At least one non-null and non-empty etl file name must be provided");
            }

            return new RawListener(nonEmptyEtls, eventRecordCallback, eventBufferCallback, true, useRawTimestamps);
        }

        /// <summary>
        /// Starts to listen for the events, this will trigger the callbacks passed on the
        /// constructor to be called.
        /// </summary>
        public void Process()
        {
            var error = NativeMethods.ProcessTrace(
                this.traceHandles,
                (uint)this.traceHandles.Length,
                IntPtr.Zero,
                IntPtr.Zero);
            if (error != NativeMethods.ErrorSuccess)
            {
                throw new Win32Exception(error);
            }
        }

        /// <summary>
        /// Disposes the associated native resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var handle in this.traceHandles)
            {
                NativeMethods.CloseTrace(handle);
            }
        }
    }
}
