// --------------------------------------------------------------------------------
// <copyright file="SessionType.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    /// <summary>
    /// Types of ETW sessions that the user can select on the configuration.
    /// </summary>
    /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363784(v=vs.85).aspx"/>
    // ReSharper disable UnusedMember.Global
    internal enum SessionType
    {
        /// <summary>
        /// ETW events are going to be logged to a file.
        /// </summary>
        File = 0,

        /// <summary>
        /// The session will be a real-time one.
        /// </summary>
        Realtime = 1,

        /// <summary>
        /// The session will be private to each process and will be logged to
        /// the respective files.
        /// </summary>
        Private = 2,

        /// <summary>
        /// The session is both a file and real-time session.
        /// </summary>
        /// <remarks>Defined also as the reverse form to facilitate parsing the enumeration value.</remarks>
        FileAndRealtime = 3,

        /// <summary>
        /// The session is both a file and real-time session.
        /// </summary>
        /// <remarks>Defined also as the reverse form to facilitate parsing the enumeration value.</remarks>
        RealtimeAndFile = 3,
    }
}