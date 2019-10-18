// -----------------------------------------------------------------------
// <copyright file="EventSourceLogEngine.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Logging
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Globalization;

    /// <summary>
    /// Log engine that will emit event source based messages.
    /// </summary>
    /// <remarks>
    /// Class is intentionally left public to allow for invocation of the GenerateManifest method if needed manifested ETW event consumption.
    /// </remarks>
    [EventSource(Name = "Microsoft-MDMetricsClient", Guid = "{FEB9BEAF-6D93-442E-BB78-7F581B618201}")]
    public sealed class EventSourceLogEngine : EventSource, ILogEngine
    {
        /// <summary>
        /// The logger instance
        /// </summary>
        private static readonly Lazy<EventSourceLogEngine> Instance = new Lazy<EventSourceLogEngine>(() => new EventSourceLogEngine());

        /// <summary>
        /// Prevents a default instance of the <see cref="EventSourceLogEngine"/> class from being created.
        /// </summary>
        private EventSourceLogEngine()
        {
            // Do nothing.
        }

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        public static EventSourceLogEngine Logger
        {
            get { return Instance.Value; }
        }

        /// <summary>
        /// Logs the given data according to the engine implementation.
        /// </summary>
        /// <param name="level">Level of the log statement.</param>
        /// <param name="logId">Log identification for classifying log statements.</param>
        /// <param name="tag">Extra string that allows another level of classification under the log id.</param>
        /// <param name="format">Message to be logged, it can be a format message.</param>
        /// <param name="objectParams">Optional, any parameter to be used to build the formatted message string.</param>
        public void Log(LoggerLevel level, object logId, string tag, string format, params object[] objectParams)
        {
            if (this.IsLogged(level, logId, tag))
            {
                var intermediateFormat = string.Format(
                    CultureInfo.InvariantCulture,
                    "Level=[{0}] LogId=[{1}] Tag=[{2}] {3}",
                    level,
                    logId,
                    tag,
                    format);

                var finalMessage = string.Format(CultureInfo.InvariantCulture, intermediateFormat, objectParams);

                switch (level)
                {
                    case LoggerLevel.Debug:
                        this.EventSourceLogDebug(finalMessage);
                        break;
                    case LoggerLevel.Info:
                        this.EventSourceLogInfo(finalMessage);
                        break;
                    case LoggerLevel.Warning:
                        this.EventSourceLogWarning(finalMessage);
                        break;
                    case LoggerLevel.Error:
                        this.EventSourceLogError(finalMessage);
                        break;
                    case LoggerLevel.CustomerFacingInfo:
                        this.EventSourceLogInfo(finalMessage);
                        break;
                }
            }
        }

        /// <summary>
        /// Logs the message via event source at debug level.
        /// </summary>
        /// <param name="message">The message.</param>
        [Event(1, Level = EventLevel.Verbose)]
        public void EventSourceLogDebug(string message)
        {
            this.WriteEvent(1, message);
        }

        /// <summary>
        /// Logs the message via event source at info level.
        /// </summary>
        /// <param name="message">The message.</param>
        [Event(2, Level = EventLevel.Informational)]
        public void EventSourceLogInfo(string message)
        {
            this.WriteEvent(2, message);
        }

        /// <summary>
        /// Logs the message via event source at warning level.
        /// </summary>
        /// <param name="message">The message.</param>
        [Event(3, Level = EventLevel.Warning)]
        public void EventSourceLogWarning(string message)
        {
            this.WriteEvent(3, message);
        }

        /// <summary>
        /// Logs the message via event source at error level.
        /// </summary>
        /// <param name="message">The message.</param>
        [Event(4, Level = EventLevel.Error)]
        public void EventSourceLogError(string message)
        {
            this.WriteEvent(4, message);
        }

        /// <summary>
        /// Checks if a log statement with the given parameters will be actually logged or
        /// not. Useful to avoid expensive operations for log statements that are going to
        /// be dropped by the log engine.
        /// </summary>
        /// <param name="level">Level of the log statement.</param>
        /// <param name="logId">Log identification for classifying log statements.</param>
        /// <param name="tag">Extra string that allows another level of classification under the log id.</param>
        /// <returns>
        /// True if the statement is going to be logged, false otherwise.
        /// </returns>
        public bool IsLogged(LoggerLevel level, object logId, string tag)
        {
            return this.IsEnabled(this.GetEtwLevelFromLogLevel(level), EventKeywords.None);
        }

        /// <summary>
        /// Gets the ETW level from log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>The ETW level</returns>
        private EventLevel GetEtwLevelFromLogLevel(LoggerLevel level)
        {
            switch (level)
            {
                case LoggerLevel.Debug:
                    return EventLevel.Verbose;
                case LoggerLevel.Info:
                    return EventLevel.Informational;
                case LoggerLevel.Warning:
                    return EventLevel.Warning;
                case LoggerLevel.Error:
                    return EventLevel.Error;
            }

            return EventLevel.Informational;
        }
    }
}
