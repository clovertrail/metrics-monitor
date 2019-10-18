// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticHeartbeatReader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.MetricsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Logging;
    using Metrics.Etw;

    /// <summary>
    /// The class for consumption of ME diagnostic heart beats.
    /// </summary>
    public sealed unsafe class DiagnosticHeartbeatReader
    {
        /// <summary>
        /// Prefix added to ETW sessions that are using the configuration of a collector.
        /// </summary>
        private const string EtwSessionsPrefix = "DiagnosticHeartbeatReader-";

        /// <summary>
        /// Custom log id to be used in the log statements.
        /// </summary>
        private static readonly object LogId = Logger.CreateCustomLogId("DiagnosticHeartbeatReader");

        /// <summary>
        /// The provider unique identifier.
        /// </summary>
        private static readonly Guid ProviderGuid = new Guid("{2F23A2A9-0DE7-4CB4-A778-FBDF5C1E7372}");

        /// <summary>
        /// Gets or sets a value indicating whether [enable verbose logging].
        /// </summary>
        public bool EnableVerboseLogging { get; set; }

        /// <summary>
        /// Reads the diagnostic heartbeats.
        /// </summary>
        /// <param name="heartbeatAction">The heartbeat action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task listening for ETW events.</returns>
        public Task ReadDiagnosticHeartbeatsAsync(
            Action<IDiagnosticHeartbeat> heartbeatAction,
            CancellationToken cancellationToken)
        {
            if (heartbeatAction == null)
            {
                throw new ArgumentNullException(nameof(heartbeatAction));
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            if (!this.EnableVerboseLogging)
            {
                Logger.SetMaxLogLevel(LoggerLevel.Error);
            }

            ActiveCollector activeCollector = null;
            Task task = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        Task.Factory.StartNew(
                            () =>
                            SetupListener(
                                ProviderGuid,
                                heartbeatAction,
                                cancellationToken,
                                out activeCollector),
                            TaskCreationOptions.LongRunning);

                        cancellationToken.WaitHandle.WaitOne();
                    }
                    finally
                    {
                        StopEtwSession(activeCollector);
                    }
                },
                TaskCreationOptions.LongRunning);

            Console.CancelKeyPress += (sender, args) => StopEtwSession(activeCollector);

            return task;
        }

        /// <summary>
        /// Stops the etw session.
        /// </summary>
        /// <param name="activeCollector">The active collector.</param>
        private static void StopEtwSession(ActiveCollector activeCollector)
        {
            if (activeCollector != null)
            {
                ActiveCollector.StopCollector(activeCollector.Name);
            }
        }

        /// <summary>
        /// Setups the listener.
        /// </summary>
        /// <param name="providerGuid">The provider unique identifier.</param>
        /// <param name="action">The action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="activeCollector">The active collector.</param>
        private static void SetupListener(
            Guid providerGuid,
            Action<IDiagnosticHeartbeat> action,
            CancellationToken cancellationToken,
            out ActiveCollector activeCollector)
        {
            var providers = new Dictionary<Guid, ProviderConfiguration>
                                {
                                    {
                                        providerGuid,
                                        new ProviderConfiguration(providerGuid, EtwTraceLevel.Verbose, 0, 0)
                                    }
                                };

            var etwSessionConfig = new CollectorConfiguration(EtwSessionsPrefix + "-")
            {
                SessionType = SessionType.Realtime,
                Providers = providers
            };

            activeCollector = new ActiveCollector(etwSessionConfig.Name);
            activeCollector.StartCollector(etwSessionConfig);

            RawListener etwListener = null;
            try
            {
                etwListener = CreateRealTimeListener(providerGuid, etwSessionConfig.Name, action, 2, cancellationToken);

                // TODO: Better to check providers periodically and retry several times.
                if (!ActiveCollector.TryUpdateProviders(etwSessionConfig))
                {
                    Logger.Log(LoggerLevel.Error, LogId, "Main", "Failed to update ETW providers. Terminating.");
                    return;
                }

                try
                {
                    etwListener.Process();
                }
                finally
                {
                    Logger.Log(
                        cancellationToken.IsCancellationRequested ? LoggerLevel.Info : LoggerLevel.Error,
                        LogId,
                        "SetupEtwDataPipeline",
                        "ETW Thread terminated unexpectedly, typically indicates that the ETW session was stopped.");
                }
            }
            finally
            {
                etwListener?.Dispose();
            }
        }

        /// <summary>
        /// Creates the real time listener.
        /// </summary>
        /// <param name="providerGuid">The provider unique identifier.</param>
        /// <param name="etlSessionConfigName">Name of the etl session configuration.</param>
        /// <param name="action">The action.</param>
        /// <param name="eventIdFilter">Event Id filter to call the action</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The ETW listener</returns>
        private static RawListener CreateRealTimeListener(
            Guid providerGuid,
            string etlSessionConfigName,
            Action<IDiagnosticHeartbeat> action,
            int eventIdFilter,
            CancellationToken cancellationToken)
        {
            return RawListener.CreateRealTimeListener(
                etlSessionConfigName,
                eventRecord =>
                {
                    if (eventRecord->EventHeader.ProviderId == providerGuid && eventRecord->EventHeader.Id == eventIdFilter)
                    {
                        action(DiagnosticHeartbeat.FromEtwEvent(eventRecord));
                    }
                },
                eventTraceLog =>
                {
                    Logger.Log(
                        LoggerLevel.Info,
                        LogId,
                        "CreateRealTimeListener",
                        "DiagnosticHeartbeat, cancelled = {0}",
                        cancellationToken.IsCancellationRequested);

                    return !cancellationToken.IsCancellationRequested;
                });
        }
    }
}