// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EtwSessionManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Types that encapsulate an ETW session.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Helper class that manages ETW sessions.
    /// </summary>
    internal static unsafe class EtwSessionManager
    {
        /// <summary>
        /// Maximum file name size.
        /// </summary>
        private const uint MaxNameSize = 1024;

        /// <summary>
        /// Size of the extension area where session and file name can be added to the end of NativeMethods.EventTraceProperties.
        /// </summary>
        /// <remarks>
        /// It accounts for 2 strings (which can be up to MaxNameSize in length).
        /// </remarks>
        private const uint ExtSize = 2 * MaxNameSize * sizeof(char);

        /// <summary>
        /// Maximum number of ETW sessions that can be handled by the QueryAllTraces API.
        /// </summary>
        private const uint MaxSessionsByQueryAllTraces = 64;

        /// <summary>
        /// Keeps the size of the managed version of the EventTraceProperties struct.
        /// </summary>
        private static readonly uint TracePropertiesSize;

        /// <summary>
        /// A shared buffer to be used by static methods to avoid allocation and de-allocation on every call.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "Not accessible to any 3rd-party MS or not")]
        private static IntPtr sharedBuffer;

        /// <summary>
        /// A object to serve as a lock for the sharing of the sharedBuffer.
        /// </summary>
        private static object sharedBufferLock = new object();

        /// <summary>
        /// Keeps track of the size of the shared buffer.
        /// </summary>
        private static int sharedBufferSize;

        /// <summary>
        /// Keeps track of the number of expected active ETW sessions when querying all sessions.
        /// </summary>
        private static uint expectedActiveEtwSessions = 32;

        /// <summary>
        /// Initializes static members of the <see cref="EtwSessionManager"/> class.
        /// </summary>
        static EtwSessionManager()
        {
            TracePropertiesSize = (uint)Marshal.SizeOf(typeof(NativeMethods.EventTraceProperties));
            sharedBufferSize = (int)(TracePropertiesSize + ExtSize);
            sharedBuffer = Marshal.AllocHGlobal(sharedBufferSize);
        }

        /// <summary>
        /// Attempts to stop the specified session exists on the system.
        /// </summary>
        /// <param name="sessionName">
        /// Name of the session to be stopped.
        /// </param>
        /// <returns>
        /// True if the given session was stopped false otherwise.
        /// </returns>
        public static bool Stop(string sessionName)
        {
            const int EtwSessionNotFound = 4201;
            int result;
            ControlTrace(sessionName, NativeMethods.TraceControl.Stop, out result);

            return result == NativeMethods.ErrorSuccess || result == EtwSessionNotFound;
        }

        /// <summary>
        /// Attempts to retrieve the properties (including status) of the given ETW session.
        /// </summary>
        /// <param name="sessionName">
        /// The name for the session which the caller wants to retrieve the properties.
        /// </param>
        /// <param name="traceProperties">
        /// The <see cref="NativeMethods.EventTraceProperties"/> instance describing the session passed
        /// by the caller. It contains valid data only if the method returns true;.
        /// </param>
        /// <returns>
        /// True if the function retrieved the session properties, false otherwise.
        /// </returns>
        public static bool TryGetSessionProperties(string sessionName, out NativeMethods.EventTraceProperties traceProperties)
        {
            int result;
            traceProperties = ControlTrace(sessionName, NativeMethods.TraceControl.Query, out result);

            return result == NativeMethods.ErrorSuccess;
        }

        /// <summary>
        /// Attempts to retrieve the active ETW sessions on the box.
        /// </summary>
        /// <returns>
        /// An array with the name of the active ETW sessions on the box.
        /// </returns>
        public static string[] GetSessionNames()
        {
            lock (sharedBufferLock)
            {
                uint actualSessions = 0;
                var retryInCaseOfSmallBuffer = true;
                int result = (int)NativeMethods.ErrorSuccess;

                while (retryInCaseOfSmallBuffer)
                {
                    var requiredSize = expectedActiveEtwSessions * (TracePropertiesSize + ExtSize);
                    if (sharedBufferSize < requiredSize)
                    {
                        ReallocateSharedBufferSize((int)requiredSize);
                    }

                    NativeMethods.ZeroMemory(sharedBuffer, requiredSize);
                    var pointerArray = new IntPtr[expectedActiveEtwSessions];
                    for (int i = 0; i < expectedActiveEtwSessions; ++i)
                    {
                        var traceProperties = (NativeMethods.EventTraceProperties*)(
                            (char*)sharedBuffer + (i * (TracePropertiesSize + ExtSize)));
                        traceProperties->LoggerNameOffset = TracePropertiesSize;
                        traceProperties->LogFileNameOffset = TracePropertiesSize + (MaxNameSize * sizeof(char));
                        traceProperties->Wnode.BufferSize = TracePropertiesSize + ExtSize;
                        pointerArray[i] = (IntPtr)traceProperties;
                    }

                    fixed (void* pointersToPropertyArray = pointerArray)
                    {
                        result = NativeMethods.QueryAllTracesW(
                            pointersToPropertyArray, expectedActiveEtwSessions, ref actualSessions);

                        // There is a bug in QueryAllTracesW: it returns success even when there wasn't space to capture
                        // all active sessions and actualSessions ends up with the same value passed as expectedActiveEtwSessions.
                        // In general QueryAllTracesW does not return ERROR_MORE_DATA when there
                        // are more sessions than EVENT_TRACE_PROPERTIES in the array, so if the returned number of
                        // actual sessions is equal to the number of expected sessions it is necessary to try again
                        // until an array with more EVENT_TRACE_PROPERTIES pointers than actual sessions is passed.
                        if ((result == NativeMethods.ErrorMoreData || expectedActiveEtwSessions == actualSessions) &&
                            expectedActiveEtwSessions < MaxSessionsByQueryAllTraces)
                        {
                            expectedActiveEtwSessions = (expectedActiveEtwSessions < actualSessions)
                                                   ? actualSessions + 1
                                                   : 2 * expectedActiveEtwSessions;

                            expectedActiveEtwSessions = Math.Max(expectedActiveEtwSessions, MaxSessionsByQueryAllTraces);
                        }
                        else
                        {
                            retryInCaseOfSmallBuffer = false;
                        }
                    }
                }

                if (result != NativeMethods.ErrorSuccess)
                {
                    throw new Win32Exception(result, "Error calling QueryAllTracesW (0x" + result.ToString("X8") + ")");
                }

                // Capture the session names from the EVENT_TRACE_PROPERTIES structures.
                var sessionNames = new string[actualSessions];
                for (int i = 0; i < actualSessions; ++i)
                {
                    var traceProperties = (char*)sharedBuffer + (int)(i * (TracePropertiesSize + ExtSize));
                    sessionNames[i] = new string(
                        traceProperties + (int)((NativeMethods.EventTraceProperties*)traceProperties)->LoggerNameOffset);
                }

                return sessionNames;
            }
        }

        /// <summary>
        /// Helper method that allows to get information about the given provider on the specified ETW session.
        /// </summary>
        /// <param name="loggerId">
        /// The id of the ETW session for which the provider settings are going to be retrieved. This id can
        /// be retrieved using the return of the GetSessionProperties method via the <c>Wnode.HistoricalContext</c>
        /// field.
        /// </param>
        /// <param name="providerId">
        /// The id of the provider for which the session settings should be retrieved.
        /// </param>
        /// <param name="enableInfo">
        /// The struct containing information with the provider in the specified session.
        /// The struct just contains valid data if the method returns true.
        /// </param>
        /// <returns>
        /// True if the provider was enabled on the session, false otherwise.
        /// </returns>
        public static bool GetProviderInfo(ulong loggerId, Guid providerId, out NativeMethods.TraceEnableInfo enableInfo)
        {
            var foundProvider = false;
            enableInfo = default(NativeMethods.TraceEnableInfo);

            lock (sharedBufferLock)
            {
                int requiredBufferSize = 0;
                void* bufferPtr = null;
                var retryInCaseOfSmallBuffer = true;
                int result = (int)NativeMethods.ErrorSuccess;

                while (retryInCaseOfSmallBuffer)
                {
                    bufferPtr = sharedBuffer.ToPointer();
                    result = NativeMethods.EnumerateTraceGuidsEx(
                        NativeMethods.TraceQueryInfoClass.TraceGuidQueryInfo,
                        &providerId,
                        sizeof(Guid),
                        bufferPtr,
                        sharedBufferSize,
                        ref requiredBufferSize);

                    if (result != NativeMethods.ErrorInsufficientBuffer)
                    {
                        retryInCaseOfSmallBuffer = false;
                    }
                    else
                    {
                        ReallocateSharedBufferSize(requiredBufferSize);
                    }
                }

                if (result == NativeMethods.ErrorWmiGuidNotFound)
                {
                    // Provider not found
                    return false;
                }

                if (result != NativeMethods.ErrorSuccess)
                {
                    throw new Win32Exception(result, "Error calling EnumerateTraceGuidsEx (0x" + result.ToString("X8") + ")");
                }

                var traceGuidInfo = *((NativeMethods.TraceGuidInfo*)bufferPtr);
                bufferPtr = ((byte*)bufferPtr) + sizeof(NativeMethods.TraceGuidInfo);

                for (int i = 0; i < traceGuidInfo.InstanceCount && !foundProvider; ++i)
                {
                    var traceProviderInstanceInfo = *((NativeMethods.TraceProviderInstanceInfo*)bufferPtr);
                    if (traceProviderInstanceInfo.EnableCount > 0)
                    {
                        var traceEnableInfoPtr = (NativeMethods.TraceEnableInfo*)(
                            (byte*)bufferPtr + sizeof(NativeMethods.TraceProviderInstanceInfo));

                        for (int j = 0; j < traceProviderInstanceInfo.EnableCount; ++j)
                        {
                            // Only add information for the expected session
                            if (traceEnableInfoPtr->LoggerId == loggerId)
                            {
                                enableInfo = *traceEnableInfoPtr;
                                foundProvider = true;
                                break;
                            }

                            traceEnableInfoPtr++;
                        }
                    }

                    bufferPtr = (byte*)bufferPtr + traceProviderInstanceInfo.NextOffset;
                }
            }

            return foundProvider;
        }

        /// <summary>
        /// Attempts to get the current ETL file name of the given ETW session.
        /// </summary>
        /// <param name="sessionName">
        /// Name of the session to retrieve the current file.
        /// </param>
        /// <param name="currentSessionFile">
        /// Name of the ETL file currently being used by the session.
        /// </param>
        /// <returns>
        /// True if the operation to retrieve the current ETL file of the session was successful, false otherwise.
        /// </returns>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Not accessible to any 3rd-party MS or not")]
        public static bool TryGetCurrentFileOfSession(string sessionName, out string currentSessionFile)
        {
            currentSessionFile = null;
            lock (sharedBufferLock)
            {
                var traceProperties = new NativeMethods.EventTraceProperties
                {
                    LoggerNameOffset = TracePropertiesSize,
                    LogFileNameOffset = TracePropertiesSize + (MaxNameSize * sizeof(char)),
                    Wnode = new NativeMethods.WnodeHeader
                    {
                        BufferSize = TracePropertiesSize + ExtSize,
                    }
                };

                NativeMethods.ZeroMemory(sharedBuffer, traceProperties.Wnode.BufferSize);
                Marshal.StructureToPtr(traceProperties, sharedBuffer, true);

                var errorCode = NativeMethods.ControlTrace(
                    0,
                    sessionName,
                    sharedBuffer,
                    (int)NativeMethods.TraceControl.Query);

                var tracePropertiesPtr = (NativeMethods.EventTraceProperties*)sharedBuffer;
                if (errorCode == NativeMethods.ErrorSuccess && tracePropertiesPtr->LogFileNameOffset > 0)
                {
                    currentSessionFile = new string((char*)(tracePropertiesPtr + (int)tracePropertiesPtr->LogFileNameOffset));
                }

                return errorCode == NativeMethods.ErrorSuccess && currentSessionFile != null;
            }
        }

        /// <summary>
        /// Sends the specified control to the given session, captures the result of the
        /// call to ControlTrace and return the struct with the trace properties (its contents
        /// depend on the control passed to the function).
        /// </summary>
        /// <param name="sessionName">
        /// Name of the session to have the control applied against.
        /// </param>
        /// <param name="traceControl">
        /// Control to be applied against the session.
        /// </param>
        /// <param name="errorCode">
        /// Output parameter that receives the result of the call to ControlTrace.
        /// </param>
        /// <returns>
        /// The struct with the trace properties (its contents depend on the control passed to the function).
        /// </returns>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Not accessible to any 3rd-party MS or not")]
        private static NativeMethods.EventTraceProperties ControlTrace(
            string sessionName, NativeMethods.TraceControl traceControl, out int errorCode)
        {
            lock (sharedBufferLock)
            {
                var traceProperties = new NativeMethods.EventTraceProperties
                {
                    LoggerNameOffset = TracePropertiesSize,
                    LogFileNameOffset = TracePropertiesSize + (MaxNameSize * sizeof(char)),
                    Wnode = new NativeMethods.WnodeHeader
                    {
                        BufferSize = TracePropertiesSize + ExtSize,
                    }
                };

                NativeMethods.ZeroMemory(sharedBuffer, traceProperties.Wnode.BufferSize);
                Marshal.StructureToPtr(traceProperties, sharedBuffer, true);

                errorCode = NativeMethods.ControlTrace(
                    0,
                    sessionName,
                    sharedBuffer,
                    (uint)traceControl);

                traceProperties = (NativeMethods.EventTraceProperties)Marshal.PtrToStructure(
                    sharedBuffer, typeof(NativeMethods.EventTraceProperties));

                return traceProperties;
            }
        }

        /// <summary>
        /// Helper that allows change in the size of the shared buffer pointer.
        /// </summary>
        /// <param name="newBufferSize">
        /// New desired buffer size.
        /// </param>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Not accessible to any 3rd-party MS or not")]
        private static void ReallocateSharedBufferSize(int newBufferSize)
        {
            lock (sharedBufferLock)
            {
                var newSize = Math.Max(sharedBufferSize, newBufferSize);
                Marshal.FreeHGlobal(sharedBuffer);
                sharedBufferSize = newSize;
                sharedBuffer = Marshal.AllocHGlobal(sharedBufferSize);
            }
        }
    }
}
