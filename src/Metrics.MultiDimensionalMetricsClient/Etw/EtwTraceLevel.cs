// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EtwTraceLevel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Enumeration with the ETW event levels.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System.Diagnostics.CodeAnalysis;

    // ReSharper disable UnusedMember.Global

    /// <summary>
    /// Enumerates the ETW trace levels used by most providers.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "Needs to have the same size as the native equivalent")]
    internal enum EtwTraceLevel : byte
    {
        /// <summary>
        /// Always log.
        /// </summary>
        LogAlways = 0x0,

        /// <summary>
        /// Critical logging only.
        /// </summary>
        Critical = 0x1,

        /// <summary>
        /// Logging errors.
        /// </summary>
        Error = 0x2,

        /// <summary>
        /// Logging warnings.
        /// </summary>
        Warning = 0x3,

        /// <summary>
        /// Informational logging.
        /// </summary>
        Informational = 0x4,

        /// <summary>
        /// Verbose logging.
        /// </summary>
        Verbose = 0x5
    }
}
