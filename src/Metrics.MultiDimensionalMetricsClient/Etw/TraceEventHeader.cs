// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TraceEventHeader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Type that wraps access to the header fields of an ETW event record.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Type that wraps access to the header fields of an ETW event record.
    /// </summary>
    internal unsafe struct TraceEventHeader
    {
        /// <summary>
        /// Pointer to the native structure being wrapped.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2151:FieldsWithCriticalTypesShouldBeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        private readonly NativeMethods.EventHeader* eventHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceEventHeader"/> struct.
        /// </summary>
        /// <param name="eventHeader">
        /// Pointer to the native structure being wrapped by the instance.
        /// </param>
        public TraceEventHeader(NativeMethods.EventHeader* eventHeader)
        {
            if (eventHeader == null)
            {
                throw new ArgumentNullException("eventHeader");
            }

            this.eventHeader = eventHeader;
        }

        /// <summary>
        /// Gets the size of the event record, in bytes.
        /// </summary>
        public ushort Size
        {
            get
            {
                return this.eventHeader->Size;
            }
        }

        /// <summary>
        /// Gets the header eventType (reserved).
        /// </summary>
        public ushort HeaderType
        {
            get
            {
                return this.eventHeader->HeaderType;
            }
        }

        /// <summary>
        /// Gets the flags that provide information about the event such as the eventType
        /// of model it was logged to and if the event contains extended data.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "As commonly used in ETW")]
        public ushort Flags
        {
            get
            {
                return this.eventHeader->Flags;
            }
        }

        /// <summary>
        /// Gets the eventType of source to use for parsing the event data.
        /// </summary>
        public ushort EventProperty
        {
            get
            {
                return this.eventHeader->EventProperty;
            }
        }

        /// <summary>
        /// Gets the thread that generated the event.
        /// </summary>
        public int ThreadId
        {
            get
            {
                return this.eventHeader->ThreadId;
            }
        }

        /// <summary>
        /// Gets the process that generated the event.
        /// </summary>
        public int ProcessId
        {
            get
            {
                return this.eventHeader->ProcessId;
            }
        }

        /// <summary>
        /// Gets the the time that the event occurred. The resolution depends on the value
        /// of the <c>Wnode.ClientContext</c> member of <c>EVENT_TRACE_PROPERTIES</c> at the time the
        /// controller created model.
        /// </summary>
        public long Timestamp
        {
            get
            {
                return this.eventHeader->TimeStamp;
            }
        }

        /// <summary>
        /// Gets the GUID that uniquely identifies the provider that logged the event.
        /// </summary>
        public Guid ProviderId
        {
            get
            {
                return this.eventHeader->ProviderId;
            }
        }

        /// <summary>
        /// Gets the Id of the event.
        /// </summary>
        public ushort Id
        {
            get
            {
                return this.eventHeader->Id;
            }
        }

        /// <summary>
        /// Gets the version of the event.
        /// </summary>
        public byte Version
        {
            get
            {
                return this.eventHeader->Version;
            }
        }

        /// <summary>
        /// Gets the channel of the event.
        /// </summary>
        public byte Channel
        {
            get
            {
                return this.eventHeader->Channel;
            }
        }

        /// <summary>
        /// Gets the level of the event.
        /// </summary>
        public byte Level
        {
            get
            {
                return this.eventHeader->Level;
            }
        }

        /// <summary>
        /// Gets the <c>opcode</c> of the event.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "As commonly used in ETW")]
        public byte Opcode
        {
            get
            {
                return this.eventHeader->Opcode;
            }
        }

        /// <summary>
        /// Gets the task of the event.
        /// </summary>
        public ushort Task
        {
            get
            {
                return this.eventHeader->Task;
            }
        }

        /// <summary>
        /// Gets the keyword of the event.
        /// </summary>
        public ulong Keyword
        {
            get
            {
                return this.eventHeader->Keyword;
            }
        }

        /// <summary>
        /// Gets the elapsed execution time for kernel-mode instructions, in CPU time units.
        /// </summary>
        public int KernelTime
        {
            get
            {
                return this.eventHeader->KernelTime;
            }
        }

        /// <summary>
        /// Gets the elapsed execution time for user-mode instructions, in CPU time units.
        /// </summary>
        public int UserTime
        {
            get
            {
                return this.eventHeader->UserTime;
            }
        }

        /// <summary>
        /// Gets an identifier that relates two events. For details, see EventWriteTransfer.
        /// </summary>
        public Guid ActivityId
        {
            get
            {
                return this.eventHeader->ActivityId;
            }
        }
    }
}
