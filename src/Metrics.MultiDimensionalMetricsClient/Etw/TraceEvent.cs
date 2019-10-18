// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TraceEvent.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Type that wraps access to the fields of an ETW event record.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Type that wraps access to the fields of an ETW event record.
    /// </summary>
    internal unsafe struct TraceEvent
    {
        /// <summary>
        /// Pointer to the native structure being wrapped.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2151:FieldsWithCriticalTypesShouldBeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        private readonly NativeMethods.EventRecord* eventRecord;

        /// <summary>
        /// Managed type that allows access to the event header.
        /// </summary>
        private readonly TraceEventHeader eventHeader;

        /// <summary>
        /// Managed type that allows access to the buffer context of the event.
        /// </summary>
        private readonly TraceBufferContext bufferContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceEvent"/> struct.
        /// </summary>
        /// <param name="eventRecord">
        /// Pointer to the native structure being wrapped by the instance.
        /// </param>
        public TraceEvent(NativeMethods.EventRecord* eventRecord)
            : this()
        {
            if (eventRecord == null)
            {
                throw new ArgumentNullException("eventRecord");
            }

            this.eventRecord = eventRecord;
            this.eventHeader = new TraceEventHeader(&eventRecord->EventHeader);
            this.bufferContext = new TraceBufferContext(&eventRecord->BufferContext);
        }

        /// <summary>
        /// Gets the header of the event.
        /// </summary>
        public TraceEventHeader Header
        {
            get
            {
                return this.eventHeader;
            }
        }

        /// <summary>
        /// Gets the buffer context of the event.
        /// </summary>
        public TraceBufferContext BufferContext
        {
            get
            {
                return this.bufferContext;
            }
        }

        /// <summary>
        /// Gets the number of extended data structures in ExtendedData.
        /// </summary>
        public ushort ExtendedDataCount
        {
            get
            {
                return this.eventRecord->ExtendedDataCount;
            }
        }

        /// <summary>
        /// Gets the Size, in bytes, of the data in UserData.
        /// </summary>
        public ushort UserDataLength
        {
            get
            {
                return this.eventRecord->UserDataLength;
            }
        }

        /// <summary>
        /// Gets the extended data items that ETW collects if the controller sets the EnableProperty
        /// parameter of EnableTraceEx. For details, see EVENT_HEADER_EXTENDED_DATA_ITEM.
        /// </summary>
        public IntPtr ExtendedData
        {
            get
            {
                return this.eventRecord->ExtendedData;
            }
        }

        /// <summary>
        /// Gets the event specific data. To parse this data, see Retrieving Event Data Using TDH.
        /// If the Flags member of EVENT_HEADER is EVENT_HEADER_FLAG_STRING_ONLY, the data is a
        /// null-terminated Unicode string that you do not need TDH to parse.
        /// </summary>
        public IntPtr UserData
        {
            get
            {
                return this.eventRecord->UserData;
            }
        }

        /// <summary>
        /// Gets the context specified in the Context member of the EVENT_TRACE_LOGFILE structure
        /// that is passed to OpenTrace.
        /// </summary>
        public IntPtr UserContext
        {
            get
            {
                return this.eventRecord->UserContext;
            }
        }
    }
}
