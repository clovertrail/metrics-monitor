// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TraceBufferContext.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Type that wraps access to the buffer context fields of an ETW event record.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Type that wraps access to the buffer context fields of an ETW event record.
    /// </summary>
    internal unsafe struct TraceBufferContext
    {
        /// <summary>
        /// Pointer to the native structure being wrapped.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2151:FieldsWithCriticalTypesShouldBeCriticalFxCopRule", Justification = "Not accessible to any 3rd-party MS or not")]
        private readonly NativeMethods.EtwBufferContext* bufferContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceBufferContext"/> struct.
        /// </summary>
        /// <param name="bufferContext">
        /// Pointer to the native structure being wrapped by the instance.
        /// </param>
        public TraceBufferContext(NativeMethods.EtwBufferContext* bufferContext)
        {
            if (bufferContext == null)
            {
                throw new ArgumentNullException("bufferContext");
            }

            this.bufferContext = bufferContext;
        }

        /// <summary>
        /// Gets the number of the CPU on which the provider process was running.
        /// The number is zero on a single processor computer.
        /// </summary>
        public byte ProcessorNumber
        {
            get
            {
                return this.bufferContext->ProcessorNumber;
            }
        }

        /// <summary>
        /// Gets alignment between events (always eight).
        /// </summary>
        public byte Alignment
        {
            get
            {
                return this.bufferContext->Alignment;
            }
        }

        /// <summary>
        /// Gets Identifier of the model that logged the event.
        /// </summary>
        public ushort LoggerId
        {
            get
            {
                return this.bufferContext->LoggerId;
            }
        }
    }
}
