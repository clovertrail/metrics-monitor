// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Logger.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Logging
{
    using System;
    using System.Linq;

    /// <summary>
    /// Static type that provides a "globally" available logging mechanism.
    /// </summary>
    /// <remarks>
    /// Implemented as a static so this does not need to be passed to every and each
    /// single type performing any logging, the actual engine doing the logging is
    /// defined via interface that should be set for this static as early as possible in
    /// the lifetime of the application.
    /// </remarks>
    public static class Logger
    {
        private static ILogEngine[] logEngines = { new ConsoleLogEngine(), EventSourceLogEngine.Logger };
        private static LoggerLevel maxLogLevel = LoggerLevel.Info;

        /// <summary>
        /// Gets or sets a value indicating whether to disable logging.
        /// </summary>
        public static bool DisableLogging { get; set; }

        /// <summary>
        /// Sets the log engines to be used for logging. This method is not
        /// thread safe, the typical scenario is for it to be called once at the
        /// start of the program and not anymore. Warning: when the log engine is changed
        /// any customer log id created via the CreateCustomLogId method should be considered
        /// invalidated and as such objects need to be re-created.
        /// </summary>
        /// <param name="logEngines">
        /// Log engines to be used to produce the logs.
        /// </param>
        public static void SetLogEngine(params ILogEngine[] logEngines)
        {
            if (logEngines == null)
            {
                throw new ArgumentNullException("logEngines");
            }

            Logger.logEngines = logEngines;
        }

        /// <summary>
        /// Sets the maximum log level, anything above this level should not be
        /// logged by the engine.
        /// </summary>
        /// <param name="level">
        /// Lower or equal to it should be logged by the engine, anything above it
        /// should not be logged.
        /// </param>
        public static void SetMaxLogLevel(LoggerLevel level)
        {
            maxLogLevel = level;
        }

        /// <summary>
        /// Checks if a log statement with the given parameters will be actually logged or
        /// not. Useful to avoid expensive operations for log statements that are going to
        /// be dropped by the log engine.
        /// </summary>
        /// <param name="level">
        /// Level of the log statement.
        /// </param>
        /// <param name="logId">
        /// Log identification for classifying log statements.
        /// </param>
        /// <param name="tag">
        /// Extra string that allows another level of classification under the log id.
        /// </param>
        /// <returns>
        /// True if the statement is going to be logged, false otherwise.
        /// </returns>
        internal static bool IsLogged(LoggerLevel level, object logId, string tag)
        {
            return (level <= maxLogLevel) && logEngines.Any(e => e.IsLogged(level, logId, tag));
        }

        /// <summary>
        /// Gets the maximum log level, anything above this level should not be
        /// logged by the engine.
        /// </summary>
        /// <returns>
        /// The maximum log level current in use by the logger.
        /// </returns>
        internal static LoggerLevel GetMaxLogLevel()
        {
            return maxLogLevel;
        }

        /// <summary>
        /// Creates a custom object used by the logger to help identifying a boundary of
        /// logging (e.g.: component, object, service, etc). Warning: these objects are
        /// specific for each implementation of a log engine. If the log engine is changed,
        /// via SetLogEngine, all previously created such objects need to be re-created for
        /// correct usage with the current log engine.
        /// </summary>
        /// <param name="logIdName">
        /// Friendly name to be associated with the log id.
        /// </param>
        /// <returns>
        /// The object to be used a log id - these objects are specific for each implementation
        /// of a log engine. If the log engine is changed, via SetLogEngine, all previously
        /// created such objects need to be re-created for correct usage with the current
        /// log engine.
        /// </returns>
        internal static object CreateCustomLogId(string logIdName)
        {
            if (string.IsNullOrEmpty(logIdName))
            {
                throw new ArgumentNullException("logIdName");
            }

            return logIdName;
        }

        /// <summary>
        /// Logs the given data according to the log engine previously set.
        /// </summary>
        /// <param name="level">
        /// Level of the log statement.
        /// </param>
        /// <param name="logId">
        /// Log identification for classifying log statements and also any object
        /// that a specific log engine may need to perform its logging. All such
        /// objects created before a call to SetLogEngine a different log engine
        /// should be re-created with the log engine currently in use.
        /// </param>
        /// <param name="tag">
        /// Extra string that allows another level of classification under the log id.
        /// </param>
        /// <param name="format">
        /// Message to be logged, it can be a format message.
        /// </param>
        /// <param name="objectParams">
        /// Optional, any parameter to be used to build the formatted message string.
        /// </param>
        internal static void Log(
            LoggerLevel level, object logId, string tag, string format, params object[] objectParams)
        {
            // Do only the cheap log level checking here, the other checking is left to the
            // log engine.
            if (level <= maxLogLevel && !DisableLogging)
            {
                foreach (var engine in logEngines)
                {
                    engine.Log(level, logId, tag, format, objectParams);
                }
            }
        }
    }
}
