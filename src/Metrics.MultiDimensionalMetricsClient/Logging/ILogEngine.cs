// -----------------------------------------------------------------------
// <copyright file="ILogEngine.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Logging
{
    using System;

    /// <summary>
    /// Level of the log messages.
    /// </summary>
    /// <remarks>
    /// Level comments taken from System.Diagnostics.Tracing.EventLevel
    /// </remarks>
    public enum LoggerLevel
    {
        /// <summary>
        /// This level adds standard errors that signify a problem.
        /// </summary>
        Error = 0,

        /// <summary>
        /// This level adds warning events (for example, events that are published
        /// because a disk is nearing full capacity).
        /// </summary>
        Warning = 1,

        /// <summary>
        /// This level adds only customer-facing infos.
        /// </summary>
        CustomerFacingInfo = 2,

        /// <summary>
        /// This level adds informational events or messages that are not errors.
        /// These events can help trace the progress or state of an application.
        /// </summary>
        Info = 3,

        /// <summary>
        /// This level adds lengthy events or messages.
        /// </summary>
        Debug = 4
    }

    /// <summary>
    /// Interface that needs to be supported by any log engine to be used by the process.
    /// </summary>
    public interface ILogEngine : IDisposable
    {
        /// <summary>
        /// Logs the given data according to the engine implementation.
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
        /// <param name="format">
        /// Message to be logged, it can be a format message.
        /// </param>
        /// <param name="objectParams">
        /// Optional, any parameter to be used to build the formatted message string.
        /// </param>
        void Log(
            LoggerLevel level,
            object logId,
            string tag,
            string format,
            params object[] objectParams);

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
        bool IsLogged(LoggerLevel level, object logId, string tag);
    }
}
