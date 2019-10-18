//---------------------------------------------------------------------------------
// <copyright file="CollectorConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Type to read the configuration of a single collector.
    /// </summary>
    internal sealed class CollectorConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectorConfiguration" /> class.
        /// </summary>
        /// <param name="etwSessionsPrefix">The etw session name prefix.</param>
        public CollectorConfiguration(string etwSessionsPrefix)
        {
            var cpuNum = Environment.ProcessorCount;

            // ETW buffer properties
            this.FlushTimerSec = 0;
            this.MinBufferCount = 2 * cpuNum;
            this.MaxBufferCount = 2 * this.MinBufferCount;
            this.BufferSizeKB = 256;
            this.ClockType = ClockType.System;
            this.SessionType = SessionType.Realtime;
            this.DeprecatedCollector = null;
            this.MaxFileSizeMB = 100;
            this.MaxFileTimeSpan = TimeSpan.FromMinutes(5);
            this.MaxFileCount = 1440;
            this.OriginalName = "Collector";
            this.Name = GetNormalizedSessionName(this.OriginalName, this.SessionType, etwSessionsPrefix);

            // Providers - by default empty
            this.Providers = new Dictionary<Guid, ProviderConfiguration>();
        }

        /// <summary>
        /// Gets the name of the ETW session to be created for this collector. This is the name found in the
        /// configuration file normalized to make possible to identify ETW sessions created by the MonitoringAgent.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the name of the collector as defined by the user in the configuration file.
        /// </summary>
        public string OriginalName { get; private set; }

        /// <summary>
        /// Gets the name of an ETW session that was used by previous versions of the dependent service that
        /// should be stopped when the service starts to use a session provided via the MonitoringAgent.
        /// </summary>
        /// <remarks>
        /// This is important when the dependant service fails to stop the ETW realtime session that was used
        /// before having the service using the session created by the MonitoringAgent. In this case there will
        /// be no listeners for the legacy session and eventually both sessions (the legacy and the one provided
        /// by the MonitoringAgent) will start to drop events.
        /// </remarks>
        public string DeprecatedCollector { get; set; }

        /// <summary>
        /// Gets how often, in seconds, the trace buffers are forcibly flushed.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363784(v=vs.85).aspx"/>
        public int FlushTimerSec { get; set; }

        /// <summary>
        /// Gets the minimum number of buffers to be allocated to the ETW session.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363784(v=vs.85).aspx"/>
        public int MinBufferCount { get; set; }

        /// <summary>
        /// Gets the maximum number of buffers to be allocated to the ETW session.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363784(v=vs.85).aspx"/>
        public int MaxBufferCount { get; set; }

        /// <summary>
        /// Gets the buffer size, in KB, to be used in the session.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363784(v=vs.85).aspx"/>
        public int BufferSizeKB { get; set; }

        /// <summary>
        /// Gets the clock type to be used in the ETW session. Check ClockType to see how this should
        /// be written in the configuration file.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa364160(v=vs.85).aspx"/>
        public ClockType ClockType { get; set; }

        /// <summary>
        /// Gets the logging mode to be used in the ETW session.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363784(v=vs.85).aspx"/>
        public SessionType SessionType { get; set; }

        /// <summary>
        /// Gets the maximum size, in megabytes, that an ETL file should be allowed to grow.
        /// </summary>
        public int MaxFileSizeMB { get; set; }

        /// <summary>
        /// Gets the maximum time that each ETL file should cover.
        /// </summary>
        public TimeSpan MaxFileTimeSpan { get; set; }

        /// <summary>
        /// Gets the maximum number of ETL files for this collector that should be allowed
        /// to exist on disk.
        /// </summary>
        public int MaxFileCount { get; set; }

        /// <summary>
        /// Gets the list of provider configurations for the collector.
        /// </summary>
        public Dictionary<Guid, ProviderConfiguration> Providers { get; set; }

        /// <summary>
        /// Gets a normalized name from the session name originally specified in the configuration. The normalized
        /// name will be the ETW session name.
        /// </summary>
        /// <param name="originalName">The session name to be normalized.</param>
        /// <param name="sessionType">The type of the session.</param>
        /// <param name="etwSessionsPrefix">The etw session name prefix.</param>
        /// <returns>
        /// The <see cref="string" /> with the normalized session name.
        /// </returns>
        /// <exception cref="System.IO.InvalidDataException">The specified session type is not recognized:  + sessionType</exception>
        public static string GetNormalizedSessionName(string originalName, SessionType sessionType, string etwSessionsPrefix)
        {
            if (originalName.Equals("NT Kernel Logger", StringComparison.OrdinalIgnoreCase))
            {
                return originalName;
            }

            string sessionTypeAbbr;
            switch (sessionType)
            {
                case SessionType.File:
                    sessionTypeAbbr = "file-";
                    break;
                case SessionType.FileAndRealtime:
                    sessionTypeAbbr = "file+live-";
                    break;
                case SessionType.Private:
                    sessionTypeAbbr = "private-";
                    break;
                case SessionType.Realtime:
                    sessionTypeAbbr = "live-";
                    break;
                default:
                    throw new InvalidDataException(
                        "The specified session type is not recognized: " + sessionType);
            }

            return etwSessionsPrefix + sessionTypeAbbr + originalName;
        }
    }
}
