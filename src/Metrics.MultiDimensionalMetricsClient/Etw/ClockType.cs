// ------------------------------------------------------------------------------------------
// <copyright file="ClockType.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    // ReSharper disable UnusedMember.Global

    /// <summary>
    /// Types of clock that can be selected for the ETW session.
    /// </summary>
    /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364160(v=vs.85).aspx"/>
    internal enum ClockType
    {
        /// <summary>
        /// The default clock type to be used by the session, it is equivalent of selecting PerformanceCounter value.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Indicates that the session uses the OS performance counter, a.k.a.: QPC. The resolution is typically 1000 times
        /// less than the CPU frequency of the box. It is the recommended way to collect high-resolution timestamps in Windows.
        /// </summary>
        Perf = 1,

        /// <summary>
        /// Indicates that the session uses the SystemTime clock (with actual resolution of ~15 milliseconds it is actually
        /// the cheaper timestamp available to ETW, the downside is the lack of resolution).
        /// </summary>
        System = 2,

        /// <summary>
        /// Indicates that the session uses the CPU timestamp (RDTSC instruction to retrieve the TSC). It is the cheapest of
        /// all with the higher resolution but not guaranteed to be in sync between different processors in the box.
        /// </summary>
        Cycle = 3,
    }
}