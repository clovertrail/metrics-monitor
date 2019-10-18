//---------------------------------------------------------------------------------
// <copyright file="ActiveCollector.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text;

    using Logging;

    /// <summary>
    /// An instance of this type represents an active collector.
    /// </summary>
    /// <remarks>
    /// The type is dissociated from its configuration since this can be updated or changed manually
    /// outside of the control of the instance (e.g.: via <c>logman</c> by the user). So this type does not
    /// carry the configuration information instead it directly queries the system for the respective
    /// ETW session whenever it needs to retrieve the current settings.
    /// </remarks>
    /// <remarks>
    /// It started to be implemented using only logman but the functionality to retrieve the providers
    /// of an ETW session would require text parsing. In order to avoid that this class also uses the
    /// PLA API.
    /// </remarks>
    internal sealed class ActiveCollector
    {
        /// <summary>
        /// Exit code value that indicates success on the call to a program.
        /// </summary>
        private const int ExitCodeSuccess = 0;

        /// <summary>
        /// Custom log id to be used in the log statements.
        /// </summary>
        private static readonly object LogId = Logger.CreateCustomLogId("ActiveCollector");

        /// <summary>
        /// Root directory where the ETL files created and manipulated via this instance are going to be created.
        /// </summary>
        private readonly string baseEtlLocation;

        /// <summary>
        /// Flag to indicate if this is a file collector or not.
        /// </summary>
        private bool isFileCollector;

        /// <summary>
        /// For collectors writing to files this keeps the base name for the ETL files.
        /// It should be used only for file collectors.
        /// </summary>
        private string etlBaseName;

        /// <summary>
        /// Keeps the maximum number of files that should be kept for collectors writing
        /// ETL files. It should be used only for file collectors.
        /// </summary>
        private int maxFileCount;

        /// <summary>
        /// Keeps the maximum size that is desired for each ETL file.
        /// It should be used only for file collectors.
        /// </summary>
        private int maxFileSizeKB;

        /// <summary>
        /// Keeps the desired maximum time span of each ETL file.
        /// It should be used only for file collectors.
        /// </summary>
        private TimeSpan maxFileTimeSpan;

        /// <summary>
        /// Gets the time that an actual file rotation was performed.
        /// </summary>
        private DateTime lastRotationTime;

        /// <summary>
        /// Keeps track of the current ETL file of the collector, it will be null if it is a real-time collector.
        /// </summary>
        private string currentEtlSessionFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveCollector"/> class.
        /// </summary>
        /// <param name="sessionName">
        /// The name of the ETW session to be used by the collector.
        /// </param>
        /// <param name="baseEtlLocation">
        /// If the collector creates ETL files this is the base location where they should be created.
        /// </param>
        public ActiveCollector(string sessionName, string baseEtlLocation = ".")
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                throw new ArgumentException("sessionName cannot be null or empty.", "sessionName");
            }

            if (string.IsNullOrEmpty(baseEtlLocation))
            {
                throw new ArgumentException("baseEtlLocation cannot be null or empty.", "baseEtlLocation");
            }

            this.Name = sessionName;
            this.lastRotationTime = DateTime.MaxValue;
            this.baseEtlLocation = baseEtlLocation;
        }

        /// <summary>
        /// Gets the name of the session associated to the collector.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Get the directory where ETL files for this collector should be written.
        /// It should be used only for file collectors.
        /// </summary>
        public string EtlLogsDirectory { get; private set; }

        /// <summary>
        /// Stops collector with the given sessionName.
        /// </summary>
        /// <param name="collectorName">
        /// Name of the collector to be stopped.
        /// </param>
        /// <returns>
        /// True if the operation succeeded, false otherwise.
        /// </returns>
        public static bool StopCollector(string collectorName)
        {
            const string MethodName = "StopCollector";

            if (string.IsNullOrEmpty(collectorName))
            {
                throw new ArgumentException("collectorName cannot be null or empty.", "collectorName");
            }

            Logger.Log(
                LoggerLevel.Info,
                LogId,
                MethodName,
                "Attempting to stop ETW session [{0}]",
                collectorName);
            if (!EtwSessionManager.Stop(collectorName))
            {
                Logger.Log(
                    LoggerLevel.Error,
                    LogId,
                    MethodName,
                    "Failed to stop ETW session [{0}]",
                    collectorName);
                return false;
            }

            Logger.Log(
                LoggerLevel.Info,
                LogId,
                MethodName,
                "ETW session [{0}] was stopped",
                collectorName);

            return true;
        }

        /// <summary>
        /// Update the collector session with the provider settings specified on the configuration.
        /// </summary>
        /// <param name="config">
        /// The configuration of the collector.
        /// </param>
        /// <returns>True if update providers succeeded.</returns>
        public static bool TryUpdateProviders(CollectorConfiguration config)
        {
            bool result = true;
            foreach (var provider in config.Providers.Values)
            {
                var args = string.Format(
                    CultureInfo.InvariantCulture,
                    "update \"{0}\" -p {1} 0x{2},0x{3} {4} -ets",
                    config.Name,
                    provider.Id.ToString("B"),
                    provider.KeywordsAny.ToString("X16"),
                    provider.KeywordsAll.ToString("X16"),
                    ((int)provider.Level).ToString(CultureInfo.InvariantCulture));
                var exitCode = RunCommand("logman", args);
                if (exitCode != ExitCodeSuccess)
                {
                    Logger.Log(
                        LoggerLevel.Error,
                        LogId,
                        "UpdateProviders",
                        "Failed to set provider {0} on collector [{1}]. Error code: 0x{2}",
                        provider.Id.ToString("B"),
                        config.Name,
                        exitCode.ToString("X8"));
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Starts the collector with the given configuration.
        /// </summary>
        /// <param name="config">
        /// The configuration of the collector to be started.
        /// </param>
        /// <returns>
        /// The complete list with the back log of ETL files associated to the collector from oldest to newest.
        /// </returns>
        public List<string> StartCollector(CollectorConfiguration config)
        {
            const string MethodName = "StartCollector";

            var etlBacklog = new List<string>();

            // If a deprecated session exists try to shut it down
            NativeMethods.EventTraceProperties traceProperties;
            if (!string.IsNullOrEmpty(config.DeprecatedCollector))
            {
                StopCollector(config.DeprecatedCollector);
            }

            var clockType = config.ClockType == ClockType.Default ? ClockType.Perf : config.ClockType;
            var preExistingSession = false;
            if (EtwSessionManager.TryGetSessionProperties(this.Name, out traceProperties))
            {
                if ((NativeMethods.EtwSessionClockType)clockType == traceProperties.Wnode.ClientContext)
                {
                    // Old session can be re-used.
                    preExistingSession = true;
                }
                else
                {
                    // Old session needs to be stopped. Note that if stop failed there is a follow up check to ensure that
                    // the error was not due to the session being stopped between the checks.
                    if (!StopCollector(this.Name))
                    {
                        Logger.Log(
                            LoggerLevel.Error,
                            LogId,
                            MethodName,
                            "Failed to stop existing trace session [{0}]. Cannot proceed to correctly update session.",
                            this.Name);
                        return etlBacklog;
                    }
                }
            }

            StringBuilder sb = new StringBuilder(256);
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0} \"{1}\" -nb {2} {3} -bs {4} -ft {5} -ct {6} -ets ",
                preExistingSession ? "update" : "start",
                config.Name,
                config.MinBufferCount.ToString(CultureInfo.InvariantCulture),
                config.MaxBufferCount.ToString(CultureInfo.InvariantCulture),
                config.BufferSizeKB.ToString(CultureInfo.InvariantCulture),
                config.FlushTimerSec.ToString(CultureInfo.InvariantCulture),
                clockType);

            if (config.SessionType == SessionType.Realtime)
            {
                sb.Append("-rt");
            }
            else
            {
                // Add parameters for proper ETL file rotation
                this.isFileCollector = true;
                this.maxFileCount = config.MaxFileCount;
                this.maxFileSizeKB = config.MaxFileSizeMB * 1024;
                this.maxFileTimeSpan = config.MaxFileTimeSpan;
                this.etlBaseName = config.OriginalName;
                this.EtlLogsDirectory = Path.Combine(this.baseEtlLocation, config.OriginalName);

                if (!ProtectedIO(
                    () => Directory.CreateDirectory(this.EtlLogsDirectory),
                    e =>
                    {
                        Logger.Log(
                            LoggerLevel.Error,
                            LogId,
                            MethodName,
                            "Failed to create directory [{0}] for collector [{1}]. Exception: {2}",
                            this.EtlLogsDirectory,
                            config.Name,
                            e);
                        Logger.Log(
                            LoggerLevel.Error,
                            LogId,
                            MethodName,
                            "Failed to create or update ETW session [{0}]",
                            config.Name);
                    }))
                {
                    return etlBacklog;
                }

                // Get the full list of available ETL files, it will be trimmed according to success of update command.
                etlBacklog = this.GetExistingEtlFiles();

                this.currentEtlSessionFile = this.GenerateNextSessionFileName();
                sb.AppendFormat(CultureInfo.InvariantCulture, "-mode Sequential -o \"{0}\"", this.currentEtlSessionFile);

                if (config.SessionType == SessionType.FileAndRealtime
                    || config.SessionType == SessionType.RealtimeAndFile)
                {
                    sb.Append(" -rt");
                }
            }

            var exitCode = RunCommand("logman", sb.ToString());
            this.lastRotationTime = DateTime.UtcNow;
            if (exitCode != ExitCodeSuccess)
            {
                Logger.Log(
                    LoggerLevel.Error,
                    LogId,
                    MethodName,
                    "Logman failed to create or update ETW session [{0}].",
                    config.Name);

                if (preExistingSession)
                {
                    if (config.SessionType != SessionType.Realtime)
                    {
                        if (!EtwSessionManager.TryGetCurrentFileOfSession(config.Name, out this.currentEtlSessionFile))
                        {
                            Logger.Log(
                                LoggerLevel.Error,
                                LogId,
                                MethodName,
                                "Failed to retrieve name of the ETL being used by ETW session [{0}].",
                                config.Name);
                        }

                        if (etlBacklog.Count > 0
                            && string.Compare(
                                etlBacklog.Last(), this.currentEtlSessionFile, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            etlBacklog.RemoveAt(etlBacklog.Count - 1);
                            Logger.Log(
                                LoggerLevel.Warning,
                                LogId,
                                MethodName,
                                "Current ETL file removed from backlog list since it is still in use. ETL File [{0}]",
                                this.currentEtlSessionFile);
                        }
                    }

                    Logger.Log(
                        LoggerLevel.Info,
                        LogId,
                        MethodName,
                        "Attempting to mitigate with pre-existing session...");
                    if (!ExistingSessionSatisfiesProviders(config))
                    {
                        Logger.Log(
                            LoggerLevel.Error,
                            LogId,
                            "StartCollector",
                            "Pre-existing session cannot be used since it does not satisfy the config.");
                    }
                    else
                    {
                        Logger.Log(
                            LoggerLevel.Error,
                            LogId,
                            MethodName,
                            "Using pre-existing ETW session since it satisfied the configuration.");
                        exitCode = ExitCodeSuccess;
                    }
                }
            }

            if (exitCode == ExitCodeSuccess || preExistingSession)
            {
                Logger.Log(
                    LoggerLevel.Info,
                    LogId,
                    MethodName,
                    "ETW session is in place call UpdateProviders to enable them.");
            }

            etlBacklog.Sort(StringComparer.OrdinalIgnoreCase);
            return etlBacklog;
        }

        /// <summary>
        /// Rotates the session ETL file according to the current client configuration, taking care of
        /// deleting old files if necessary. Returns true if a file was actually rotated.
        /// </summary>
        /// <param name="referenceTime">
        /// The time for which the method should calculate if a file needs to be rotated or not.
        /// </param>
        /// <param name="closedSessionFile">
        /// Output parameter that will receive the name of the ETL file that was just closed in case
        /// an actual file rotation was performed. It is meaningful only if the method returns true.
        /// </param>
        /// <returns>
        /// True if an actual file rotation was performed, otherwise it returns false.
        /// </returns>
        public bool RotateSessionFile(DateTime referenceTime, out string closedSessionFile)
        {
            var rotatedFile = false;
            closedSessionFile = null;

            if (this.isFileCollector)
            {
                // The rotation can be triggered in two conditions:
                // 1. If the time for the current file passed
                // 2. If the current size of the file exceeds the threshold
                var attemptRotation =
                    ((referenceTime - this.lastRotationTime) > this.maxFileTimeSpan) ||
                    (this.GetCurrentFileSize() > this.maxFileSizeKB);

                if (attemptRotation)
                {
                    var nextFileName = this.GenerateNextSessionFileName();
                    var args = string.Format(CultureInfo.InvariantCulture, "update \"{0}\" -o \"{1}\" -ets", this.Name, nextFileName);
                    var exitCode = RunCommand("logman", args);
                    if (exitCode == ExitCodeSuccess)
                    {
                        rotatedFile = true;
                        closedSessionFile = this.currentEtlSessionFile;
                        this.currentEtlSessionFile = nextFileName;
                        this.lastRotationTime = DateTime.UtcNow;
                        this.DeleteOlderSessionFiles();
                    }
                }
            }

            return rotatedFile;
        }

        /// <summary>
        /// Checks if an existing ETW session satisfies the given collector configuration.
        /// </summary>
        /// <param name="config">
        /// Configuration that should be checked against the existing ETW session.
        /// </param>
        /// <returns>
        /// True if the existing session satisfies the given configuration, false otherwise.
        /// </returns>
        internal static bool ExistingSessionSatisfiesProviders(CollectorConfiguration config)
        {
            // Ok, the session is already in place, just check the providers
            bool providersOk = false;
            NativeMethods.EventTraceProperties sessionProperties;
            if (EtwSessionManager.TryGetSessionProperties(config.Name, out sessionProperties))
            {
                providersOk = true;

                // Loop over providers and ensure that they are good
                var loggerId = sessionProperties.Wnode.HistoricalContext;
                foreach (var provider in config.Providers)
                {
                    NativeMethods.TraceEnableInfo providerInSession;
                    providersOk = EtwSessionManager.GetProviderInfo(loggerId, provider.Value.Id, out providerInSession)
                                  && providerInSession.IsEnabled != 0
                                  && provider.Value.Level <= (EtwTraceLevel)providerInSession.Level
                                  && (provider.Value.KeywordsAll | providerInSession.MatchAllKeyword) == provider.Value.KeywordsAll
                                  && (provider.Value.KeywordsAny & providerInSession.MatchAnyKeyword) == provider.Value.KeywordsAny;

                    if (!providersOk)
                    {
                        // pass the native type to a provider to get a nice string representing the
                        // current settings
                        var providerConfigInSession = new ProviderConfiguration(
                            provider.Value.Id,
                            (EtwTraceLevel)providerInSession.Level,
                            providerInSession.MatchAnyKeyword,
                            providerInSession.MatchAllKeyword);
                        Logger.Log(
                            LoggerLevel.Error,
                            LogId,
                            "ExistingSessionSatisfiesProviders",
                            "Provider configuration [{0}] is not satisfied in ETW session [{1}]. Actual provider settings in session: {2}",
                            provider.Value,
                            config.Name,
                            providerInSession.IsEnabled == 0 ? "provider not enabled" : providerConfigInSession.ToString());
                    }
                }
            }

            return providersOk;
        }

        /// <summary>
        /// Disables a specific provider in a given collector.
        /// </summary>
        /// <param name="collectorName">
        ///     The sessionName of the collector in which the provider should be disabled.
        /// </param>
        /// <param name="providerId">
        ///     The Id of provider that should be disabled.
        /// </param>
        private static void DisableProvider(string collectorName, Guid providerId)
        {
            var args = string.Format(
                CultureInfo.InvariantCulture,
                "update \"{0}\" --p {1} -ets",
                collectorName,
                providerId.ToString("B"));

            // Ignore any error when stopping a provider, information already logged inside RunCommand
            RunCommand("logman", args);
        }

        /// <summary>
        /// Executes a command-line application and returns its exit code.
        /// </summary>
        /// <param name="fileName">
        /// Name of the application to be executed.
        /// </param>
        /// <param name="arguments">
        /// Command-line arguments to be passed to the application.
        /// </param>
        /// <returns>
        /// Exit code returned by the application.
        /// </returns>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Not accessible to any 3rd-party MS or not")]
        private static int RunCommand(string fileName, string arguments)
        {
            const string MethodName = "RunCommand";

            Logger.Log(
                LoggerLevel.Info,
                LogId,
                MethodName,
                "[{0} {1}]",
                fileName,
                arguments);

            using (var p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = fileName;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.CreateNoWindow = true;

                p.Start();
                while (!p.StandardOutput.EndOfStream)
                {
                    string line = p.StandardOutput.ReadLine();
                    Logger.Log(LoggerLevel.Info, LogId, MethodName, "\t\t{0}", line);
                }

                var win32Exception = new Win32Exception(p.ExitCode);
                Logger.Log(
                    LoggerLevel.Info,
                    LogId,
                    MethodName,
                    "exitCode=0x{0}: {1}",
                    p.ExitCode.ToString("X8"),
                    win32Exception.Message);

                return p.ExitCode;
            }
        }

        /// <summary>
        /// Performs an IO operation catching all IO, security and unauthorized access
        /// exceptions.
        /// </summary>
        /// <param name="ioOperation">
        /// IO operation to be performed.
        /// </param>
        /// <param name="failureAction">
        /// Action to be executed in case an exception is raised by the IO operation.
        /// Note that this action takes the raised exception was a parameter.
        /// </param>
        /// <returns>
        /// True if the IO operation did not raise any exception, false otherwise.
        /// </returns>
        private static bool ProtectedIO(Action ioOperation, Action<Exception> failureAction)
        {
            Exception exception = null;
            try
            {
                ioOperation();
            }
            catch (IOException e)
            {
                exception = e;
            }
            catch (SecurityException e)
            {
                exception = e;
            }
            catch (UnauthorizedAccessException e)
            {
                exception = e;
            }

            if (exception != null)
            {
                failureAction(exception);
            }

            return exception == null;
        }

        /// <summary>
        /// Builds the name of the next ETL file.
        /// </summary>
        /// <returns>
        /// The name for the next ETL for the session.
        /// </returns>
        private string GenerateNextSessionFileName()
        {
            // ATTENTION: during rotation of ETL files the deletion code assumes that if the ETL files
            // are sorted according to their names (ordinal and ignore case) the older ones are going
            // to be the first ones. Be careful if changing the resulting file name.
            var dateTime = DateTime.UtcNow;
            var nextSessionFileName = string.Format(
                CultureInfo.CurrentCulture,
                "{0}_{1:0000}-{2:00}-{3:00}_{4:00}-{5:00}-{6:00}_utc.etl",
                this.etlBaseName,
                dateTime.Year,
                dateTime.Month,
                dateTime.Day,
                dateTime.Hour,
                dateTime.Minute,
                dateTime.Second);
            return Path.Combine(this.EtlLogsDirectory, nextSessionFileName);
        }

        /// <summary>
        /// Deletes older session files associated to the collector.
        /// </summary>
        /// <remarks>
        /// In order for this method to work correctly it depends that a sort by file names
        /// should produce the same sequence as sorting the files by their creation date.
        /// </remarks>
        private void DeleteOlderSessionFiles()
        {
            if (Directory.Exists(this.EtlLogsDirectory))
            {
                List<string> existingFiles = this.GetExistingEtlFiles();

                var totalFilesToDelete = existingFiles.Count - this.maxFileCount;
                if (totalFilesToDelete > 0)
                {
                    // ATTENTION: Sorting file names should give the older files in front of the list
                    existingFiles.Sort(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < totalFilesToDelete; ++i)
                    {
                        var existingFile = existingFiles[i];
                        ProtectedIO(
                            () => File.Delete(existingFile),
                            e => Logger.Log(
                                    LoggerLevel.Warning,
                                    LogId,
                                    "DeleteOlderSessionFiles",
                                    "Failed to delete ETL file [{0}]. Exception: {1}",
                                    existingFile,
                                    e.Message));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the ETL files associated to this collector.
        /// </summary>
        /// <returns>
        /// The list of ETL files generated by the collector so far.
        /// </returns>
        private List<string> GetExistingEtlFiles()
        {
            List<string> existingFiles = null;
            var searchPattern = this.etlBaseName + "_*.etl";

            // ReSharper disable ImplicitlyCapturedClosure
            if (!ProtectedIO(
                    () => existingFiles = Directory.GetFiles(this.EtlLogsDirectory, searchPattern).ToList(),
                    e =>
                    Logger.Log(
                        LoggerLevel.Warning,
                        LogId,
                        "GetExistingEtlFiles",
                        "Failed to enumerate files at [{0}] with mask [{1}] for collector [{2}]. Exception: {3}",
                        this.EtlLogsDirectory,
                        searchPattern,
                        this.Name,
                        e.Message)))
            {
                return new List<string>();
            }

            // ReSharper restore ImplicitlyCapturedClosure
            return existingFiles;
        }

        /// <summary>
        /// Gets the size of the current ETL file, in kilobytes, being written by the collector.
        /// </summary>
        /// <returns>
        /// The current size of the ETL file being written by the collector.
        /// </returns>
        private uint GetCurrentFileSize()
        {
            if (!this.isFileCollector)
            {
                throw new InvalidOperationException("Tried to obtain file size for the non-file collector [" + this.Name + "]");
            }

            throw new NotImplementedException();
        }
    }
}
