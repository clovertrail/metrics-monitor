// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalMetricReader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Etw;
    using Logging;

    /// <summary>
    /// The class for consumption of locally aggregated metrics or local raw metrics.
    /// </summary>
    public sealed unsafe class LocalMetricReader : ILocalMetricReader
    {
        /// <summary>
        /// Prefix added to ETW sessions that are using the configuration of a collector.
        /// </summary>
        private const string EtwSessionsPrefix = "LocalMetricReader-";

        /// <summary>
        /// Custom log id to be used in the log statements.
        /// </summary>
        private static readonly object LogId = Logger.CreateCustomLogId("LocalMetricReader");

        /// <summary>
        /// The aggregated metrics provider unique identifier.
        /// </summary>
        private static readonly Guid AggregatedMetricsProviderGuid = new Guid("{2F23A2A9-0DE7-4CB4-A778-FBDF5C1E7372}");

        /// <summary>
        /// The raw metrics etw provider unique identifier.
        /// </summary>
        private static readonly Guid RawMetricsEtwProviderGuid = new Guid("{EDC24920-E004-40F6-A8E1-0E6E48F39D84}");

        /// <summary>
        /// Gets or sets a value indicating whether [enable verbose logging].
        /// </summary>
        public bool EnableVerboseLogging { get; set; }

        /// <summary>
        /// Reads the local raw metrics.
        /// </summary>
        /// <param name="metricProducedAction">The action to execute when metric available.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="etlFileName">The name of the etw file from when read data. If null, realtime session will be used.</param>
        /// <returns>
        /// An awaitable <see cref="Task" />.
        /// </returns>
        public Task ReadLocalRawMetricsAsync(
            Action<ILocalRawMetric> metricProducedAction,
            CancellationToken cancellationToken,
            string etlFileName = null)
        {
            if (metricProducedAction == null)
            {
                throw new ArgumentNullException("metricProducedAction");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
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
                                SetupRawMetricListener(
                                    RawMetricsEtwProviderGuid,
                                    etlFileName,
                                    metricProducedAction,
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
        /// Reads the locally aggregated metrics.
        /// </summary>
        /// <param name="metricProducedAction">The action to execute when metric available.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="etlFileName">The name of the etw file from when read data. If null, realtime session will be used.</param>
        /// <returns>
        /// An awaitable <see cref="Task" />.
        /// </returns>
        public Task ReadLocalAggregatedMetricsAsync(
            Action<ILocalAggregatedMetric> metricProducedAction,
            CancellationToken cancellationToken,
            string etlFileName = null)
        {
            if (metricProducedAction == null)
            {
                throw new ArgumentNullException("metricProducedAction");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
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
                                SetupAggregatedMetricListener(
                                    AggregatedMetricsProviderGuid,
                                    etlFileName,
                                    metricProducedAction,
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
        /// Setups the raw metric listener.
        /// </summary>
        /// <param name="providerGuid">The provider unique identifier.</param>
        /// <param name="etlFileName">The name of the etw file from when read data. If null, realtime session will be used.</param>
        /// <param name="metricProducedAction">The when metric available.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="activeCollector">The active collector.</param>
        private static void SetupRawMetricListener(
            Guid providerGuid,
            string etlFileName,
            Action<ILocalRawMetric> metricProducedAction,
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

            var etwSessionConfig = new CollectorConfiguration(EtwSessionsPrefix + "raw-")
                                       {
                                           SessionType = SessionType.Realtime,
                                           Providers = providers
                                       };

            activeCollector = new ActiveCollector(etwSessionConfig.Name);
            activeCollector.StartCollector(etwSessionConfig);

            RawListener etwListener = null;
            try
            {
                etwListener = string.IsNullOrWhiteSpace(etlFileName)
                                  ? CreateRealTimeListener(providerGuid, etwSessionConfig.Name, metricProducedAction, cancellationToken)
                                  : CreateFileListener(providerGuid, etlFileName, metricProducedAction, cancellationToken);

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
                if (etwListener != null)
                {
                    etwListener.Dispose();
                }
            }
        }

        /// <summary>
        /// Setups the aggregated metric listener.
        /// </summary>
        /// <param name="providerGuid">The provider unique identifier.</param>
        /// <param name="etlFileName">The name of the etw file from when read data. If null, realtime session will be used.</param>
        /// <param name="metricProducedAction">The when metric available.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="activeCollector">The active collector.</param>
        private static void SetupAggregatedMetricListener(
            Guid providerGuid,
            string etlFileName,
            Action<ILocalAggregatedMetric> metricProducedAction,
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

            var etwSessionConfig = new CollectorConfiguration(EtwSessionsPrefix + "aggregated-")
                                       {
                                           SessionType = SessionType.Realtime,
                                           Providers = providers
                                       };

            activeCollector = new ActiveCollector(etwSessionConfig.Name);
            activeCollector.StartCollector(etwSessionConfig);

            RawListener etwListener = null;
            try
            {
                etwListener = string.IsNullOrWhiteSpace(etlFileName)
                                  ? CreateRealTimeListener(providerGuid, etwSessionConfig.Name, metricProducedAction, 1, cancellationToken)
                                  : CreateFileListener(providerGuid, etlFileName, metricProducedAction, 1, cancellationToken);

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
                if (etwListener != null)
                {
                    etwListener.Dispose();
                }
            }
        }

        /// <summary>
        /// Creates the real time listener.
        /// </summary>
        /// <param name="providerGuid">The provider unique identifier.</param>
        /// <param name="etlSessionConfigName">Name of the etl session configuration.</param>
        /// <param name="metricProducedAction">The metric produced action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="RawListener"/>.</returns>
        private static RawListener CreateRealTimeListener(
            Guid providerGuid,
            string etlSessionConfigName,
            Action<ILocalRawMetric> metricProducedAction,
            CancellationToken cancellationToken)
        {
            return RawListener.CreateRealTimeListener(
                etlSessionConfigName,
                eventRecord =>
                    {
                        if (eventRecord->EventHeader.ProviderId == providerGuid)
                        {
                            metricProducedAction(LocalRawMetric.ConvertToMetricData(eventRecord));
                        }
                    },
                eventTraceLog =>
                    {
                        Logger.Log(
                            LoggerLevel.Info,
                            LogId,
                            "CreateRealTimeListener",
                            "LocalRawMetric, cancelled = {0}",
                            cancellationToken.IsCancellationRequested);

                        return !cancellationToken.IsCancellationRequested;
                    });
        }

        /// <summary>
        /// Creates the file listener.
        /// </summary>
        /// <param name="providerGuid">The provider unique identifier.</param>
        /// <param name="etlFileName">Name of the etl file.</param>
        /// <param name="metricProducedAction">The metric produced action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An instance of <see cref="RawListener"/>.</returns>
        private static RawListener CreateFileListener(
            Guid providerGuid,
            string etlFileName,
            Action<ILocalRawMetric> metricProducedAction,
            CancellationToken cancellationToken)
        {
            return RawListener.CreateEtlFileListener(
                new[] { etlFileName },
                eventRecord =>
                    {
                        if (eventRecord->EventHeader.ProviderId == providerGuid)
                        {
                            metricProducedAction(LocalRawMetric.ConvertToMetricData(eventRecord));
                        }
                    },
                eventTraceLog =>
                    {
                        Logger.Log(
                            LoggerLevel.Info,
                            LogId,
                            "CreateFileListener",
                            "LocalRawMetric, cancelled = {0}",
                            cancellationToken.IsCancellationRequested);

                        return !cancellationToken.IsCancellationRequested;
                    });
        }

        /// <summary>
        /// Creates the real time listener.
        /// </summary>
        /// <param name="providerGuid">The provider unique identifier.</param>
        /// <param name="etlSessionConfigName">Name of the etl session configuration.</param>
        /// <param name="metricProducedAction">The metric produced action.</param>
        /// <param name="eventIdFilter">The event identifier filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="RawListener"/> instance.</returns>
        private static RawListener CreateRealTimeListener(
            Guid providerGuid,
            string etlSessionConfigName,
            Action<ILocalAggregatedMetric> metricProducedAction,
            int eventIdFilter,
            CancellationToken cancellationToken)
        {
            return RawListener.CreateRealTimeListener(
                etlSessionConfigName,
                eventRecord =>
                    {
                        if (eventRecord->EventHeader.ProviderId == providerGuid && eventRecord->EventHeader.Id == eventIdFilter)
                        {
                            metricProducedAction(LocalAggregatedMetric.ConvertToMetricData(eventRecord));
                        }
                    },
                eventTraceLog =>
                    {
                        Logger.Log(
                            LoggerLevel.Info,
                            LogId,
                            "CreateRealTimeListener",
                            "LocalAggregatedMetric, cancelled = {0}",
                            cancellationToken.IsCancellationRequested);

                        return !cancellationToken.IsCancellationRequested;
                    });
        }

        /// <summary>
        /// Creates the file listener.
        /// </summary>
        /// <param name="providerGuid">The provider unique identifier.</param>
        /// <param name="etlFileName">Name of the etl file.</param>
        /// <param name="metricProducedAction">The metric produced action.</param>
        /// <param name="eventIdFilter">The event identifier filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="RawListener"/> instance.</returns>
        private static RawListener CreateFileListener(
            Guid providerGuid,
            string etlFileName,
            Action<ILocalAggregatedMetric> metricProducedAction,
            int eventIdFilter,
            CancellationToken cancellationToken)
        {
            return RawListener.CreateEtlFileListener(
                new[] { etlFileName },
                eventRecord =>
                    {
                        if (eventRecord->EventHeader.ProviderId == providerGuid && eventRecord->EventHeader.Id == eventIdFilter)
                        {
                            metricProducedAction(LocalAggregatedMetric.ConvertToMetricData(eventRecord));
                        }
                    },
                eventTraceLog =>
                    {
                        Logger.Log(
                            LoggerLevel.Info,
                            LogId,
                            "CreateFileListener",
                            "LocalAggregatedMetric, cancelled = {0}",
                            cancellationToken.IsCancellationRequested);

                        return !cancellationToken.IsCancellationRequested;
                    });
        }
    }
}