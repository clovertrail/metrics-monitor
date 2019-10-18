// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Native methods required to interact with ETW.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    // ReSharper disable FieldCanBeMadeReadOnly.Global
    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable UnusedMember.Global

    /// <summary>
    /// This type contains the P/Invoke declarations needed to interact with ETW.
    /// </summary>
    [SuppressMessage("Microsoft.Security", "CA2111:PointersShouldNotBeVisible", Justification = "Not accessible to any 3rd-party MS or not")]
    internal static class NativeMethods
    {
        /// <summary>
        /// Managed version for ERROR_SUCCESS. Windows error code that represents no error, i.e.: success.
        /// </summary>
        internal const uint ErrorSuccess = 0;

        /// <summary>
        /// Managed version for ERROR_INSUFFICIENT_BUFFER. The data area passed to a system call is too small.
        /// </summary>
        internal const uint ErrorInsufficientBuffer = 122;

        /// <summary>
        /// Managed version for ERROR_ALREADY_EXISTS. Cannot create some object because it already exists.
        /// </summary>
        internal const uint ErrorAlreadyExists = 183;

        /// <summary>
        /// Managed version for ERROR_MORE_DATA. More data is available.
        /// </summary>
        internal const uint ErrorMoreData = 234;

        /// <summary>
        /// Managed version for ERROR_WMI_GUID_NOT_FOUND. The GUID passed was not recognized as valid by a WMI data provider.
        /// </summary>
        internal const uint ErrorWmiGuidNotFound = 4200;

        /// <summary>
        /// Managed version for ERROR_WMI_INSTANCE_NOT_FOUND. The instance name passed was not recognized as valid by a WMI data provider.
        /// </summary>
        internal const uint ErrorWmiInstanceNotFound = 4201;

        /// <summary>
        /// Managed version for INVALID_HANDLE_VALUE. Win32 constant the represents an invalid handle.
        /// </summary>
        internal const ulong InvalidHandleValue = unchecked((ulong)(-1));

        /// <summary>
        /// Managed version for INVALID_TRACEHANDLE_64. Represents an invalid trace handle for 64bit apps.
        /// </summary>
        internal const ulong InvalidTracehandle64 = unchecked((ulong)(-1));

        /// <summary>
        /// Managed version for INVALID_TRACEHANDLE_32. Represents an invalid trace handle for 32bit apps.
        /// </summary>
        internal const ulong InvalidTracehandle32 = 0x00000000FFFFFFFF;

        /// <summary>
        /// Managed version for PROCESS_TRACE_MODE_REAL_TIME. Indicates to ETW to process the trace in
        /// real-time (live) mode.
        /// </summary>
        internal const uint ProcessTraceModeRealTime = 0x00000100;

        /// <summary>
        /// Managed version for PROCESS_TRACE_MODE_RAW_TIMESTAMP. Indicates to ETW to return the event
        /// timestamps with their raw value instead of transforming them to FILETIME.
        /// </summary>
        internal const uint ProcessTraceModeRawTimestamp = 0x00001000;

        /// <summary>
        /// Managed version for PROCESS_TRACE_MODE_EVENT_RECORD. Indicates to ETW to callback for each
        /// event using the modern (Crimson) event format.
        /// </summary>
        internal const uint ProcessTraceModeEventRecord = 0x10000000;

        /// <summary>
        /// Managed version for EVENT_TRACE_REAL_TIME_MODE. Indicates to ETW to process the trace in real
        /// time (live) mode.
        /// </summary>
        internal const uint EventTraceRealTimeMode = 0x00000100;

        /// <summary>
        /// Managed version for EVENT_TRACE_FILE_MODE_SEQUENTIAL. Indicates to ETW that the ETW events
        /// should be written sequentially to a file.
        /// </summary>
        internal const uint EventTraceFileModeSequential = 0x00000001;

        /// <summary>
        /// Managed version for EVENT_TRACE_PRIVATE_LOGGER_MODE. Indicates to ETW that the session should
        /// be in the private logger mode.
        /// </summary>
        internal const uint EventTracePrivateLoggerMode = 0x00000800;

        /// <summary>
        /// Managed version for EVENT_TRACE_INDEPENDENT_SESSION_MODE. Indicates that a logging session should
        /// not be affected by EventWrite failures in other sessions. Without this flag, if an event cannot be
        /// published to one of the sessions that a provider is enabled to, the event will not get published
        /// to any of the sessions. When this flag is set, a failure to write an event to one session will not
        /// cause the EventWrite function to return an error code in other sessions.
        /// </summary>
        /// <remarks>
        /// Per e-mail thread with ETW owners there is no adverse impact (memory, latency, etc).
        /// </remarks>
        internal const uint EventTraceIndependentSessionMode = 0x08000000;

        /// <summary>
        /// Managed version for EVENT_TRACE_CONTROL_QUERY. Control code used by ETW to query the
        /// properties of a tracing session.
        /// </summary>
        internal const uint EventTraceControlQuery = 0;

        /// <summary>
        /// Managed version for EVENT_TRACE_CONTROL_STOP. Control code used by ETW to stop a tracing
        /// session.
        /// </summary>
        internal const uint EventTraceControlStop = 1;

        /// <summary>
        /// Managed version for EVENT_TRACE_CONTROL_UPDATE. Control code used by ETW to update the
        /// properties of a tracing session.
        /// </summary>
        internal const uint EventTraceControlUpdate = 2;

        /// <summary>
        /// Managed version for EVENT_CONTROL_CODE_ENABLE_PROVIDER. Control code used by ETW to
        /// enable providers.
        /// </summary>
        internal const uint EventControlCodeEnableProvider = 1;

        /// <summary>
        /// Managed version for WNODE_FLAG_TRACED_GUID. Value used to indicate the the structure
        /// contains event tracing information.
        /// </summary>
        internal const uint WnodeFlagTracedGuid = 0x00020000;

        /// <summary>
        /// Managed delegate that represents the BufferCallback Win32 callback function.
        /// </summary>
        /// <param name="eventTraceLog">
        /// It is really an EventTraceLogFile type with information about the buffer, however it is more efficient to marshal
        /// it manually.
        /// </param>
        /// <returns>
        /// True if the processing of the trace should continue, false to terminate the processing.
        /// </returns>
        public delegate bool EventTraceBufferCallback(
            [In] IntPtr eventTraceLog);

        /// <summary>
        /// Managed delegate that represents the EventRecordCallback Win32 callback function.
        /// </summary>
        /// <param name="rawData">
        /// Pointer to a EventRecord instance with the event record.
        /// </param>
        public unsafe delegate void EventRecordCallback(
            [In] EventRecord* rawData);

        /// <summary>
        /// Clock resolution to use when logging the time stamp for each event.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "Original type used in native WinAPI is uint")]
        public enum EtwSessionClockType : uint
        {
            /// <summary>
            /// The default clock type to be used by the session, it is equivalent of selecting PerformanceCounter value.
            /// </summary>
            Default = 0,

            /// <summary>
            /// Indicates that the session uses the OS performance counter, a.k.a.: QPC. The resolution is typically 1000 times
            /// less than the CPU frequency of the box. It is the recommended way to collect high-resolution timestamps in Windows.
            /// </summary>
            PerformanceCounter = 1,

            /// <summary>
            /// Indicates that the session uses the SystemTime clock (with actual resolution of ~15 milliseconds it is actually
            /// the cheaper timestamp available to ETW, the downside is the lack of resolution).
            /// </summary>
            SystemTime = 2,

            /// <summary>
            /// Indicates that the session uses the CPU timestamp (RDTSC instruction to retrieve the TSC). It is the cheapest of
            /// all with the higher resolution but not guaranteed to be in sync between different processors in the box.
            /// </summary>
            CpuTimestamp = 3,
        }

        /// <summary>
        /// Control codes to be used with the <c>ControlTrace</c> Windows API.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "Original type used in native WinAPI is uint")]
        public enum TraceControl : uint
        {
            /// <summary>
            /// Managed version for EVENT_TRACE_CONTROL_QUERY. Control code used by ETW to query the
            /// properties of a tracing session.
            /// </summary>
            Query = 0,

            /// <summary>
            /// Managed version for EVENT_TRACE_CONTROL_STOP. Control code used by ETW to stop a tracing
            /// session.
            /// </summary>
            Stop = 1,

            /// <summary>
            /// Managed version for EVENT_TRACE_CONTROL_UPDATE. Control code used by ETW to update the
            /// properties of a tracing session.
            /// </summary>
            Update = 2,

            /// <summary>
            /// Managed version for EVENT_TRACE_CONTROL_FLUSH. Control code used by ETW to update the
            /// properties of a tracing session.
            /// </summary>
            Flush = 3,
        }

        /// <summary>
        /// Mirrors the native TRACE_QUERY_INFO_CLASS enumerations.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364147(v=vs.85).aspx"/>
        internal enum TraceQueryInfoClass
        {
            /// <summary>
            /// Query an array of GUIDs of the providers that are registered on the computer.
            /// </summary>
            TraceGuidQueryList,

            /// <summary>
            /// Query information that each session used to enable the provider.
            /// </summary>
            TraceGuidQueryInfo,

            /// <summary>
            /// Query an array of GUIDs of the providers that registered themselves in the same
            /// process as the calling process.
            /// </summary>
            TraceGuidQueryProcess,

            /// <summary>
            /// Query the setting for call stack tracing for kernel events.
            /// The value is supported on Windows 7, Windows Server 2008 R2, and later.
            /// </summary>
            TraceStackTracingInfo,

            /// <summary>
            /// Query the setting for the EnableFlags for the system trace provider. For more information,
            /// see the EVENT_TRACE_PROPERTIES structure.
            /// The value is supported on Windows 8, Windows Server 2012, and later.
            /// </summary>
            TraceSystemTraceEnableFlagsInfo,

            /// <summary>
            /// Queries the setting for the sampling profile interval for the supplied source.
            /// The value is supported on Windows 8, Windows Server 2012, and later.
            /// </summary>
            TraceSampledProfileIntervalInfo,

            /// <summary>
            /// Query which sources will be traced.
            /// The value is supported on Windows 8, Windows Server 2012, and later.
            /// </summary>
            TraceProfileSourceConfigInfo,

            /// <summary>
            /// Query the setting for sampled profile list information.
            /// The value is supported on Windows 8, Windows Server 2012, and later.
            /// </summary>
            TraceProfileSourceListInfo,

            /// <summary>
            /// Query the list of system events on which performance monitoring counters will be collected.
            /// The value is supported on Windows 8, Windows Server 2012, and later.
            /// </summary>
            TracePmcEventListInfo,

            /// <summary>
            /// Query the list of performance monitoring counters to collect.
            /// The value is supported on Windows 8, Windows Server 2012, and later.
            /// </summary>
            TracePmcCounterListInfo,

            /// <summary>
            /// Marks the last value in the enumeration. Do not use.
            /// </summary>
            MaxTraceSetInfoClass
        }

        /// <summary>
        /// P/Invoke declaration for <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364117(v=vs.85).aspx">
        /// StartTrace</see>.
        /// </summary>
        /// <param name="sessionHandle">
        /// Handle to the event tracing session.
        /// </param>
        /// <param name="sessionName">
        /// Name of the session.
        /// </param>
        /// <param name="properties">
        /// Properties of the session.
        /// </param>
        /// <returns>
        /// The Win32 error code of the call (zero indicates success, i.e. ERROR_SUCCESS Win32 error code).
        /// </returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        internal static extern int StartTrace(
            [Out] out ulong sessionHandle,
            [In] string sessionName,
            [In][Out] IntPtr properties);

        /// <summary>
        /// P/Invoke declaration for <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363696(v=vs.85).aspx">
        /// ControlTrace</see>.
        /// </summary>
        /// <param name="sessionHandle">
        /// Handle of the event tracing session.
        /// </param>
        /// <param name="sessionName">
        /// Name of the event tracing session.
        /// </param>
        /// <param name="properties">
        /// Properties of the session.
        /// </param>
        /// <param name="controlCode">
        /// Control code being passed to the session.
        /// </param>
        /// <returns>
        /// The Win32 error code of the call (zero indicates success, i.e. ERROR_SUCCESS Win32 error code).
        /// </returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        internal static extern int ControlTrace(
            [In] ulong sessionHandle,
            [In] string sessionName,
            [In][Out] IntPtr properties,
            [In] uint controlCode);

        /// <summary>
        /// P/Invoke declaration for <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/dd392305(v=vs.85).aspx">
        /// EnableTraceEx2</see>.
        /// </summary>
        /// <param name="traceHandle">
        /// Handle to the trace session to which the provider is going to be enabled or disabled.
        /// </param>
        /// <param name="providerGuid">
        /// Id of the provider to be enabled or disabled.
        /// </param>
        /// <param name="controlCode">
        /// Control code to be passed to the provider.
        /// </param>
        /// <param name="level">
        /// Importance level for which the provider is going to be set (only events with this severity and higher will be
        /// collected).
        /// </param>
        /// <param name="matchAnyKeyword">
        /// Events that will be collected need to match at least one of the bits in the keyword.
        /// </param>
        /// <param name="matchAllKeyword">
        /// Only events matching all the bits in the keyword will be collected.
        /// </param>
        /// <param name="timeoutMilliseconds">
        /// Timeout in milliseconds for the call to the method.
        /// </param>
        /// <param name="enableParameters">
        /// Parameters used to enable the provider<see href="http://msdn.microsoft.com/en-us/library/windows/desktop/dd392306(v=vs.85).aspx"/>.
        /// </param>
        /// <returns>
        /// The Win32 error code of the call (zero indicates success, i.e. ERROR_SUCCESS Win32 error code).
        /// </returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        internal static extern int EnableTraceEx2(
            [In] ulong traceHandle,
            [In] ref Guid providerGuid,
            [In] uint controlCode,
            [In] byte level,
            [In] ulong matchAnyKeyword,
            [In] ulong matchAllKeyword,
            [In] uint timeoutMilliseconds,
            [In][Optional] IntPtr enableParameters);

        /// <summary>
        /// P/Invoke declaration for ZeroMemory Win32 function.
        /// </summary>
        /// <param name="handle">
        /// Handle to the memory to be zeroed.
        /// </param>
        /// <param name="length">
        /// Number of bytes that should be zeroed.
        /// </param>
        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        internal static extern void ZeroMemory(IntPtr handle, uint length);

        /// <summary>
        /// P/Invoke declaration for the <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364089(v=vs.85).aspx">
        /// OpenTrace</see> function.
        /// </summary>
        /// <param name="traceLog">
        /// Type with the information about the trace to be opened.
        /// </param>
        /// <returns>
        /// If successful it returns a handle to the trace, otherwise a INVALID_PROCESSTRACE_HANDLE (note that this handle is
        /// different if the process is running as a Windows on Windows).
        /// </returns>
        [DllImport(
            "advapi32.dll",
            EntryPoint = "OpenTraceW",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        internal static extern ulong OpenTrace(
            [In][Out] ref EventTraceLogfilew traceLog);

        /// <summary>
        /// P/Invoke declaration for the <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364093(v=vs.85).aspx">
        /// ProcessTrace</see> function.
        /// </summary>
        /// <param name="handleArray">
        /// Array to with the handles of all traces to be processed.
        /// </param>
        /// <param name="handleCount">
        /// Counter of the handles in the array.
        /// </param>
        /// <param name="startTime">
        /// The start time for which one wants to receive events from the traces.
        /// </param>
        /// <param name="endTime">
        /// The end time for which one wants to stop receiving events from the traces.
        /// </param>
        /// <returns>
        /// It returns 0 (ERROR_SUCCESS) in case of success and Win32 system error code in case of error.
        /// </returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        internal static extern int ProcessTrace(
            [In] ulong[] handleArray,
            [In] uint handleCount,
            [In] IntPtr startTime,
            [In] IntPtr endTime);

        /// <summary>
        /// P/Invoke declaration for<see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363686(v=vs.85).aspx">CloseTrace</see> function.
        /// </summary>
        /// <param name="traceHandle">
        /// The trace handle to be closed.
        /// </param>
        /// <returns>
        /// It returns 0 (ERROR_SUCCESS) in case of success and Win32 system error code in case of error.
        /// </returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        internal static extern int CloseTrace(
            [In] ulong traceHandle);

        /// <summary>
        /// P/Invoke declaration for EnumerateTraceGuidsEx.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363714(v=vs.85).aspx"/>
        /// <param name="traceQueryInfoClass">
        /// Determines the type of information to include with the list of registered providers.
        /// For possible values, see the TRACE_QUERY_INFO_CLASS enumeration.
        /// </param>
        /// <param name="inBuffer">
        /// GUID of the provider whose information you want to retrieve. Specify the GUID only
        /// if TraceQueryInfoClass is TraceGuidQueryInfo.
        /// </param>
        /// <param name="inBufferSize">
        /// Size, in bytes, of the data InBuffer.
        /// </param>
        /// <param name="outBuffer">
        /// Application-allocated buffer that contains the enumerated information. The format of
        /// the information depends on the value of TraceQueryInfoClass. For details, see Remarks.
        /// </param>
        /// <param name="outBufferSize">
        /// Size, in bytes, of the OutBuffer buffer. If the function succeeds, the ReturnLength
        /// parameter receives the size of the buffer used. If the buffer is too small, the function
        /// returns ERROR_INSUFFICIENT_BUFFER and the ReturnLength parameter receives the required
        /// buffer size. If the buffer size is zero on input, no data is returned in the buffer and
        /// the ReturnLength parameter receives the required buffer size.
        /// </param>
        /// <param name="returnLength">
        /// Actual size of the data in OutBuffer, in bytes.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is ERROR_SUCCESS. If the function fails,
        /// the return value is one of the system error codes.
        /// </returns>
        [DllImport("advapi32.dll")]
        [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        internal static extern unsafe int EnumerateTraceGuidsEx(
            TraceQueryInfoClass traceQueryInfoClass,
            void* inBuffer,
            int inBufferSize,
            void* outBuffer,
            int outBufferSize,
            ref int returnLength);

        /// <summary>
        /// P/Invoke declaration for QueryAllTraces.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364102(v=vs.85).aspx"/>
        /// <param name="propertyArray">
        /// An array of pointers to EVENT_TRACE_PROPERTIES structures that receive session properties and
        /// statistics for the event tracing sessions.
        /// </param>
        /// <param name="propertyArrayCount">
        /// Number of structures in the PropertyArray array.
        /// </param>
        /// <param name="sessionCount">
        /// Actual number of event tracing sessions started on the computer.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is ERROR_SUCCESS. If the function fails,
        /// the return value is one of the system error codes.
        /// </returns>
        [DllImport("advapi32.dll")]
        [SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        internal static extern unsafe int QueryAllTracesW(
            [In][Out] void* propertyArray,
            [In] uint propertyArrayCount,
            [In][Out] ref uint sessionCount);

        /// <summary>
        /// Managed version if WNODE_HEADER.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WnodeHeader
        {
            /// <summary>
            /// Gets the total size of memory allocated, in bytes, for the event tracing
            /// model Properties. The size of memory must include the room for the
            /// EVENT_TRACE_PROPERTIES structure plus the model name string and log file
            /// name string that follow the structure in memory.
            /// </summary>
            public uint BufferSize;

            /// <summary>
            /// Gets a value reserved for internal use.
            /// </summary>
            public uint ProviderId;

            /// <summary>
            /// Gets, on output, the handle to the event tracing model.
            /// </summary>
            public ulong HistoricalContext;

            /// <summary>
            /// Gets the time at which the information in this structure was updated,
            /// in 100-nanosecond intervals since midnight, January 1, 1601.
            /// </summary>
            public ulong TimeStamp;

            /// <summary>
            /// Gets the GUID of the model.
            /// </summary>
            public Guid Guid;

            /// <summary>
            /// Gets the clock resolution to use when logging the time stamp for each
            /// event. The default is query performance counter (QPC).
            /// </summary>
            public EtwSessionClockType ClientContext;

            /// <summary>
            /// Gets the flags of the model.
            /// </summary>
            /// <remarks>
            /// Must contain WNODE_FLAG_TRACED_GUID to indicate that the structure
            /// contains event tracing information.
            /// </remarks>
            public uint Flags;
        }

        /// <summary>
        /// Managed version of EVENT_TRACE_PROPERTIES. Note that it cannot be used directly with the P/Invoke functions because
        /// extra information is added to the end of the struct (see LogFileNameOffset and LoggerNameOffset).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct EventTraceProperties
        {
            /// <summary>
            /// Gets the WNODE_HEADER structure associated to the trace.
            /// </summary>
            public WnodeHeader Wnode;

            /// <summary>
            /// Gets the amount of memory allocated for each event tracing model
            /// buffer, in kilobytes.
            /// </summary>
            public uint BufferSize;

            /// <summary>
            /// Gets the minimum number of buffers allocated for the event tracing
            /// model's buffer pool.
            /// </summary>
            public uint MinimumBuffers;

            /// <summary>
            /// Gets the maximum number of buffers allocated for the event tracing
            /// model's buffer pool.
            /// </summary>
            public uint MaximumBuffers;

            /// <summary>
            /// Gets the maximum size of the file used to log events, in megabytes.
            /// </summary>
            public uint MaximumFileSize;

            /// <summary>
            /// Gets the logging file mode for the trace.
            /// </summary>
            public uint LogFileMode;

            /// <summary>
            /// Gets the time to wait before flushing buffers, in seconds. If zero,
            /// ETW flushes buffers as soon as they become full. If non-zero, ETW
            /// flushes all buffers that contain events based on the timer value.
            /// </summary>
            public uint FlushTimer;

            /// <summary>
            /// Gets, via bit flags, which events are enabled for a kernel logger model.
            /// </summary>
            public uint EnableFlags;

            /// <summary>
            /// Gets a value that is not used by ETW.
            /// </summary>
            public int AgeLimit;

            /// <summary>
            /// Gets, on output, the number of buffers allocated for the event tracing
            /// model's buffer pool.
            /// </summary>
            public uint NumberOfBuffers;

            /// <summary>
            /// Gets, on output, the number of buffers that are allocated but unused
            /// in the event tracing model's buffer pool.
            /// </summary>
            public uint FreeBuffers;

            /// <summary>
            /// Gets, on output, the number of events that were not recorded.
            /// </summary>
            public uint EventsLost;

            /// <summary>
            /// Gets, on output, the number of buffers written.
            /// </summary>
            public uint BuffersWritten;

            /// <summary>
            /// Gets, on output, the number of buffers that could not be written
            /// to the log file.
            /// </summary>
            public uint LogBuffersLost;

            /// <summary>
            /// Gets, on output, the number of buffers that could not be delivered
            /// in real-time to the consumer.
            /// </summary>
            public uint RealTimeBuffersLost;

            /// <summary>
            /// Gets, on output, the thread identifier for the event tracing model.
            /// </summary>
            [SuppressMessage("Microsoft.Security", "CA2111:PointersShouldNotBeVisible", Justification = "Not accessible to any 3rd-party MS or not")]
            public IntPtr LoggerThreadId;

            /// <summary>
            /// Gets the offset from the start of the structure's allocated memory to
            /// beginning of the null-terminated string that contains the log file name.
            /// </summary>
            public uint LogFileNameOffset;

            /// <summary>
            /// Gets the offset from the start of the structure's allocated memory to
            /// beginning of the null-terminated string that contains the model name.
            /// </summary>
            public uint LoggerNameOffset;
        }

        /// <summary>
        /// Managed version of ETW_BUFFER_CONTEXT, contains some context information about the ETW buffer in which
        /// the event was collected.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("Microsoft.Security", "CA2151:FieldsWithCriticalTypesShouldBeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        public struct EtwBufferContext
        {
            /// <summary>
            /// Gets the number of the CPU on which the provider process was running.
            /// The number is zero on a single processor computer.
            /// </summary>
            public byte ProcessorNumber;

            /// <summary>
            /// Gets alignment between events (always eight).
            /// </summary>
            public byte Alignment;

            /// <summary>
            /// Gets Identifier of the model that logged the event.
            /// </summary>
            public ushort LoggerId;
        }

        /// <summary>
        /// Simplified managed version of EVENT_HEADER.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("Microsoft.Security", "CA2151:FieldsWithCriticalTypesShouldBeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        public struct EventHeader
        {
            /// <summary>
            /// Gets the size of the event record, in bytes.
            /// </summary>
            public ushort Size;

            /// <summary>
            /// Gets the header eventType (reserved).
            /// </summary>
            public ushort HeaderType;

            /// <summary>
            /// Gets the flags that provide information about the event such as the eventType
            /// of model it was logged to and if the event contains extended data.
            /// </summary>
            public ushort Flags;            // offset: 0x4

            /// <summary>
            /// Gets the eventType of source to use for parsing the event data.
            /// </summary>
            public ushort EventProperty;

            /// <summary>
            /// Gets the thread that generated the event.
            /// </summary>
            public int ThreadId;            // offset: 0x8

            /// <summary>
            /// Gets the process that generated the event.
            /// </summary>
            public int ProcessId;           // offset: 0xc

            /// <summary>
            /// Gets the  time the event occurred. The resolution depends on the value
            /// of the <see href="http://msdn.microsoft.com/en-us/library/aa364160(v=vs.85).aspx">ClientContext</see> of
            /// the WNODE_HEADER member of the EVENT_TRACE_PROPERTIES structure when the controller created the session.
            /// </summary>
            public long TimeStamp;          // offset: 0x10

            /// <summary>
            /// Gets the GUID that uniquely identifies the provider that logged the event.
            /// </summary>
            public Guid ProviderId;         // offset: 0x18

            /// <summary>
            /// Gets the Id of the event.
            /// </summary>
            public ushort Id;               // offset: 0x28

            /// <summary>
            /// Gets the version of the event.
            /// </summary>
            public byte Version;            // offset: 0x2a

            /// <summary>
            /// Gets the channel of the event.
            /// </summary>
            public byte Channel;

            /// <summary>
            /// Gets the level of the event.
            /// </summary>
            public byte Level;              // offset: 0x2c

            /// <summary>
            /// Gets the opcode of the event.
            /// </summary>
            [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
            public byte Opcode;

            /// <summary>
            /// Gets the task of the event.
            /// </summary>
            public ushort Task;

            /// <summary>
            /// Gets the keyword of the event.
            /// </summary>
            public ulong Keyword;

            /// <summary>
            /// Gets the elapsed execution time for kernel-mode instructions, in CPU time units.
            /// </summary>
            public int KernelTime;         // offset: 0x38

            /// <summary>
            /// Gets the elapsed execution time for user-mode instructions, in CPU time units.
            /// </summary>
            public int UserTime;           // offset: 0x3C

            /// <summary>
            /// Gets an identifier that relates two events. For details, see EventWriteTransfer.
            /// </summary>
            public Guid ActivityId;
        }

        /// <summary>
        /// Managed the version of EVENT_RECORD, represents a single modern (a.k.a.: Crimson) ETW event.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("Microsoft.Security", "CA2151:FieldsWithCriticalTypesShouldBeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        public struct EventRecord
        {
            /// <summary>
            /// Gets the header information about the event such as the time stamp for when
            /// it was written. For details, see EVENT_HEADER.
            /// </summary>
            public EventHeader EventHeader;    // size: 80

            /// <summary>
            /// Gets the context information such as the model that logged the event.
            /// For details, see ETW_BUFFER_CONTEXT.
            /// </summary>
            public EtwBufferContext BufferContext;    // size: 4

            /// <summary>
            /// Gets the number of extended data structures in ExtendedData.
            /// </summary>
            public ushort ExtendedDataCount;

            /// <summary>
            /// Gets the Size, in bytes, of the data in UserData.
            /// </summary>
            public ushort UserDataLength;       // offset: 86

            /// <summary>
            /// Gets the extended data items that ETW collects if the controller sets the EnableProperty
            /// parameter of EnableTraceEx. For details, see EVENT_HEADER_EXTENDED_DATA_ITEM.
            /// </summary>
            [SuppressMessage("Microsoft.Security", "CA2111:PointersShouldNotBeVisible", Justification = "Not accessible to any 3rd-party MS or not")]
            public IntPtr ExtendedData;

            /// <summary>
            /// Gets the event specific data. To parse this data, see Retrieving Event Data Using TDH.
            /// If the Flags member of EVENT_HEADER is EVENT_HEADER_FLAG_STRING_ONLY, the data is a
            /// null-terminated Unicode string that you do not need TDH to parse.
            /// </summary>
            [SuppressMessage("Microsoft.Security", "CA2111:PointersShouldNotBeVisible", Justification = "Not accessible to any 3rd-party MS or not")]
            public IntPtr UserData;

            /// <summary>
            /// Gets the context specified in the Context member of the EVENT_TRACE_LOGFILE structure
            /// that is passed to OpenTrace.
            /// </summary>
            [SuppressMessage("Microsoft.Security", "CA2111:PointersShouldNotBeVisible", Justification = "Not accessible to any 3rd-party MS or not")]
            public IntPtr UserContext;
        }

        /// <summary>
        /// Managed declaration for the native TRACE_ENABLE_INFO structure.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364141(v=vs.85).aspx"/>
        [StructLayout(LayoutKind.Sequential)]
        public struct TraceEnableInfo
        {
            /// <summary>
            /// Indicates if the provider is enabled to the session. The value is TRUE if the provider
            /// is enabled to the session, otherwise, the value is FALSE. This value should always be TRUE.
            /// </summary>
            public uint IsEnabled;

            /// <summary>
            /// Level of detail that the session asked the provider to include in the events. For details,
            /// see the Level parameter of the EnableTraceEx function.
            /// </summary>
            public byte Level;

            /// <summary>
            /// Reserved, do not use.
            /// </summary>
            public byte Reserved1;

            /// <summary>
            /// Identifies the session that enabled the provider.
            /// </summary>
            public ushort LoggerId;

            /// <summary>
            /// Additional information that the session wants ETW to include in the log file. For details,
            /// see the EnableProperty parameter of the EnableTraceEx function.
            /// </summary>
            public uint EnableProperty;

            /// <summary>
            /// Reserved, do not use.
            /// </summary>
            public uint Reserved2;

            /// <summary>
            /// Keywords specify which events the session wants the provider to write. For details, see the
            /// MatchAnyKeyword parameter of the EnableTraceEx function.
            /// </summary>
            public long MatchAnyKeyword;

            /// <summary>
            /// Keywords specify which events the session wants the provider to write. For details, see the
            /// MatchAllKeyword parameter of the EnableTraceEx function.
            /// </summary>
            public long MatchAllKeyword;
        }

        /// <summary>
        /// Managed version of TIME_ZONE_INFORMATION. Used as one field of TRACE_EVENT_LOGFILE, below.
        /// Total struct size is 0xac.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Size = 0xac, CharSet = CharSet.Unicode)]
        internal struct TimeZoneInformation
        {
            /// <summary>
            /// Gets the current bias for local time translation on this computer, in
            /// minutes. The bias is the difference, in minutes, between Coordinated
            /// Universal Time (UTC) and local time.
            /// </summary>
            internal uint bias;

            /// <summary>
            /// Gets the description for standard time. For example, "EST" could
            /// indicate Eastern Standard Time.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string standardName;

            /// <summary>
            /// Gets the A SYSTEMTIME structure that contains a date and local time
            /// when the transition from daylight saving time to standard time occurs
            /// on this operating system.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 8)]
            internal ushort[] standardDate;

            /// <summary>
            /// Gets the bias value to be used during local time translations that
            /// occur during standard time. This member is ignored if a value for
            /// the StandardDate member is not supplied.
            /// </summary>
            internal uint standardBias;

            /// <summary>
            /// Gets a description for daylight saving time. For example, "PDT" could
            /// indicate Pacific Daylight Time.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string daylightName;

            /// <summary>
            /// Gets a SYSTEMTIME structure that contains a date and local time when
            /// the transition from standard time to daylight saving time occurs on
            /// this operating system.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 8)]
            internal ushort[] daylightDate;

            /// <summary>
            /// Gets the bias value to be used during local time translations that
            /// occur during daylight saving time. This member is ignored if a value
            /// for the DaylightDate member is not supplied.
            /// </summary>
            internal uint daylightBias;
        }

        /// <summary>
        /// Managed version of TRACE_LOGFILE_HEADER is used to define EVENT_TRACE_LOGFILEW.
        /// Total struct size is 0x110.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TraceLogfileHeader
        {
            /// <summary>
            /// Gets the size of the event tracing model's buffers, in kilobytes.
            /// </summary>
            internal uint BufferSize;

            /// <summary>
            /// Gets the version number of the operating system. This is a roll-up of
            /// the members of VersionDetail. Starting with the low-order bytes, the
            /// first two bytes contain MajorVersion, the next two bytes contain MinorVersion,
            /// the next two bytes contain SubVersion, and the last two bytes contain SubMinorVersion.
            /// </summary>
            internal uint Version;

            /// <summary>
            /// Gets the build number of the operating system.
            /// </summary>
            internal uint ProviderVersion;

            /// <summary>
            /// Gets the number of processors on the system.
            /// </summary>
            internal uint NumberOfProcessors;

            /// <summary>
            /// Gets the time at which the event tracing model stopped, in 100-nanosecond
            /// intervals since midnight, January 1, 1601. This value may be 0 if you are
            /// consuming events in real time or from a log file to which the provide is
            /// still logging events.
            /// </summary>
            internal long EndTime;            // 0x10

            /// <summary>
            /// Gets the time at which the event tracing model stopped, in 100-nanosecond
            /// intervals since midnight, January 1, 1601. This value may be 0 if you are
            /// consuming events in real time or from a log file to which the provide is
            /// still logging events.
            /// </summary>
            internal uint TimerResolution;

            /// <summary>
            /// Gets the maximum size of the log file, in megabytes.
            /// </summary>
            internal uint MaximumFileSize;

            /// <summary>
            /// Gets the current logging mode for the event tracing model.
            /// </summary>
            internal uint LogFileMode;

            /// <summary>
            /// Gets the total number of buffers written by the event tracing model.
            /// </summary>
            internal uint BuffersWritten;

            /// <summary>
            /// Gets a reserved value.
            /// </summary>
            internal uint StartBuffers;

            /// <summary>
            /// Gets the size of a pointer data eventType, in bytes.
            /// </summary>
            internal uint PointerSize;

            /// <summary>
            /// Gets the number of events lost during the event tracing model. Events
            /// may be lost due to insufficient memory or a very high rate of incoming
            /// events.
            /// </summary>
            internal uint EventsLost;         // 0x30

            /// <summary>
            /// Gets the CPU speed, in MHz.
            /// </summary>
            internal uint CpuSpeedInMHz;

            /// <summary>
            /// Gets a value that is not used (present only to keep the struct layout).
            /// </summary>
            internal IntPtr LoggerName; // string, but not CoTaskMemAlloc'd

            /// <summary>
            /// Gets a value that is not used (present only to keep the struct layout).
            /// </summary>
            internal IntPtr LogFileName; // string, but not CoTaskMemAlloc'd

            /// <summary>
            /// Gets A TIME_ZONE_INFORMATION structure that contains the time zone
            /// for the BootTime, EndTime and StartTime members.
            /// </summary>
            internal TimeZoneInformation TimeZone;   // 0x40         0xac size

            /// <summary>
            /// Gets the time at which the system was started, in 100-nanosecond intervals
            /// since midnight, January 1, 1601. BootTime is supported only for traces
            /// written to the Global Logger model.
            /// </summary>
            internal long BootTime;

            /// <summary>
            /// Gets the frequency of the high-resolution performance counter, if one exists.
            /// </summary>
            internal long PerfFreq;

            /// <summary>
            /// Gets the time at which the event tracing model started, in 100-nanosecond
            /// intervals since midnight, January 1, 1601.
            /// </summary>
            internal long StartTime;

            /// <summary>
            /// Gets the the clock eventType. For details, see the ClientContext member of WNODE_HEADER.
            /// </summary>
            internal uint ReservedFlags;

            /// <summary>
            /// Gets the total number of buffers lost during the event tracing model.
            /// </summary>
            internal uint BuffersLost;
        }

        /// <summary>
        /// Managed version of EVENT_TRACE_HEADER, represents the common header of all ETW events.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EventTraceHeader
        {
            /// <summary>
            /// Gets the total number of bytes of the event. Size includes the size
            /// of the header structure, plus the size of any event-specific data
            /// appended to the header.
            /// </summary>
            internal ushort Size;

            /// <summary>
            /// Gets a reserved value.
            /// </summary>
            internal ushort FieldTypeFlags; // holds our MarkerFlags too

            /// <summary>
            /// Gets the eventType of event. A provider can define their own event types
            /// or use the predefined event types.
            /// </summary>
            internal byte Type;

            /// <summary>
            /// Gets the provider-defined value that defines the severity level used
            /// to generate the event. The value ranges from 0 to 255.
            /// </summary>
            internal byte Level;

            /// <summary>
            /// Gets the version of the event trace class that you are using to log
            /// the event. Specify zero if there is only one version of your event
            /// trace class. The version tells the consumer which MOF class to use
            /// to decipher the event data.
            /// </summary>
            internal ushort Version;

            /// <summary>
            /// Gets the id of the thread that generated the event.
            /// </summary>
            internal int ThreadId;

            /// <summary>
            /// Gets the id of the process that generated the event.
            /// </summary>
            internal int ProcessId;

            /// <summary>
            /// Gets the  time the event occurred. The resolution depends on the value
            /// of the <see href="http://msdn.microsoft.com/en-us/library/aa364160(v=vs.85).aspx">ClientContext</see> of
            /// the WNODE_HEADER member of the EVENT_TRACE_PROPERTIES structure when the controller created the session.
            /// </summary>
            internal long TimeStamp;          // Offset 0x10

            /// <summary>
            /// Gets the Event trace class GUID. You can use the class GUID to identify
            /// a category of events and the Class.EventType member to identify an event within
            /// the category of events.
            /// </summary>
            internal Guid Guid;

            /// <summary>
            /// Gets the elapsed execution time for kernel-mode instructions, in CPU time
            /// units. If you are using a private model, use the value in the ProcessorTime
            /// member instead.
            /// </summary>
            internal int KernelTime;         // Offset 0x28

            /// <summary>
            /// Gets the elapsed execution time for user-mode instructions, in CPU time units.
            /// If you are using a private model, use the value in the ProcessorTime member
            /// instead.
            /// </summary>
            internal int UserTime;
        }

        /// <summary>
        /// Managed version of EVENT_TRACE, it represents a single ETW event.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EventTrace
        {
            /// <summary>
            /// Gets an EVENT_TRACE_HEADER structure that contains standard event
            /// tracing information.
            /// </summary>
            internal EventTraceHeader Header;

            /// <summary>
            /// Gets the instance identifier. Contains valid data when the provider
            /// calls the TraceEventInstance function to generate the event.
            /// Otherwise, the value is zero.
            /// </summary>
            internal uint InstanceId;

            /// <summary>
            /// Gets the instance identifier for a parent event. Contains valid data
            /// when the provider calls the TraceEventInstance function to generate
            /// the event. Otherwise, the value is zero.
            /// </summary>
            internal uint ParentInstanceId;

            /// <summary>
            /// Gets the GUID of the parent event. Contains valid data when the provider
            /// calls the TraceEventInstance function to generate the event.
            /// Otherwise, the value is zero.
            /// </summary>
            internal Guid ParentGuid;

            /// <summary>
            /// Gets the pointer to the beginning of the event-specific data for this event.
            /// </summary>
            internal IntPtr MofData; // PVOID

            /// <summary>
            /// Gets the number of bytes pointed by <see cref="MofData"/>.
            /// </summary>
            internal int MofLength;

            /// <summary>
            /// Gets information about the event such as the model identifier and
            /// processor number of the CPU on which the provider process ran.
            /// </summary>
            internal EtwBufferContext BufferContext;
        }

        /// <summary>
        /// Managed version of EVENT_TRACE_LOGFILEW. This is the main struct passed to OpenTrace().
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct EventTraceLogfilew
        {
            /// <summary>
            /// Gets the name of the log file used by the event tracing model.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string LogFileName;

            /// <summary>
            /// Gets the name of the event tracing model.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string LoggerName;

            /// <summary>
            /// Gets the current time, in 100-nanosecond intervals since midnight, January 1, 1601.
            /// </summary>
            internal long CurrentTime;

            /// <summary>
            /// Gets the number of buffers processed.
            /// </summary>
            internal uint BuffersRead;

            /// <summary>
            /// Gets the log file mode (real time or trace) to be used.
            /// </summary>
            internal uint LogFileMode;

            /// <summary>
            /// Gets, on output, an EVENT_TRACE structure that contains the last event processed.
            /// </summary>
            /// <remarks>
            /// EVENT_TRACE for the current event.  Nulled-out when we are opening files.
            /// </remarks>
            internal EventTrace CurrentEvent;

            /// <summary>
            /// Gets, on output, a TRACE_LOGFILE_HEADER structure that contains general
            /// information about the model and the computer on which the model ran.
            /// </summary>
            internal TraceLogfileHeader LogfileHeader;

            /// <summary>
            /// Gets the pointer to the BufferCallback function that receives buffer-related
            /// statistics for each buffer ETW flushes. ETW calls this callback after it delivers
            /// all the events in the buffer. This callback is optional.
            /// </summary>
            internal EventTraceBufferCallback BufferCallback;

            /// <summary>
            /// Gets, on output, the size of each buffer, in bytes.
            /// </summary>
            internal int BufferSize;

            /// <summary>
            /// Gets, on output, contains the number of bytes in the buffer that contain valid information.
            /// </summary>
            internal int Filled;

            /// <summary>
            /// Gets the number of lost events. Currently not used by ETW.
            /// </summary>
            internal int EventsLost;

            /// <summary>
            /// Gets the pointer to the EventCallback function that ETW calls for each event in the buffer.
            /// </summary>
            internal EventRecordCallback EventCallback;

            /// <summary>
            /// Gets a value indicating whether this is a kernel trace or not.
            /// </summary>
            internal int IsKernelTrace;

            /// <summary>
            /// Gets the context data that a consumer can specify when calling OpenTrace.
            /// </summary>
            internal IntPtr Context;
        }

        /// <summary>
        /// Managed declaration for the native TRACE_GUID_INFO structure.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364142(v=vs.85).aspx"/>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TraceGuidInfo
        {
            /// <summary>
            /// The number of TRACE_PROVIDER_INSTANCE_INFO blocks contained in the list. You can have
            /// multiple instances of the same provider if the provider lives in a DLL that is loaded
            /// by multiple processes.
            /// </summary>
            internal uint InstanceCount;

            /// <summary>
            /// Reserved, do not use.
            /// </summary>
            internal uint Reserved;
        }

        /// <summary>
        /// Managed declaration for the native TRACE_PROVIDER_INSTANCE_INFO structure.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364146(v=vs.85).aspx"/>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TraceProviderInstanceInfo
        {
            /// <summary>
            /// Offset, in bytes, from the beginning of this structure to the next TRACE_PROVIDER_INSTANCE_INFO
            /// structure. The value is zero if there is not another instance info block.
            /// </summary>
            internal uint NextOffset;

            /// <summary>
            /// Number of TRACE_ENABLE_INFO structures in this block. Each structure represents a session that
            /// enabled the provider.
            /// </summary>
            internal uint EnableCount;

            /// <summary>
            /// Process identifier of the process that registered the provider.
            /// </summary>
            internal uint Pid;

            /// <summary>
            /// Can be one of the following flags TRACE_PROVIDER_FLAG_LEGACY, i.e.: The provider used
            /// RegisterTraceGuids instead of EventRegister to register itself, or
            /// TRACE_PROVIDER_FLAG_PRE_ENABLE, i.e.: The provider is not registered; however, one or
            /// more sessions have enabled the provider.
            /// </summary>
            internal uint Flags;
        }
    }
}