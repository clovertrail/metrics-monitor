// -----------------------------------------------------------------------
// <copyright file="ConsoleLogEngine.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Logging
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Log engine that sends its output to console.
    /// </summary>
    internal sealed class ConsoleLogEngine : ILogEngine
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
        public void Log(LoggerLevel level, object logId, string tag, string format, params object[] objectParams)
        {
            if (level == LoggerLevel.CustomerFacingInfo)
            {
                var customerFacingLog = string.Format(
                    CultureInfo.InvariantCulture,
                    "[{0}] {1}",
                    DateTime.UtcNow.ToString("hh:mm:ss"),
                    format);

                Console.BackgroundColor = ConsoleColor.Black;

                if (objectParams == null || objectParams.Length == 0)
                {
                    Console.WriteLine(customerFacingLog);
                }
                else
                {
                    Console.WriteLine(customerFacingLog, objectParams);
                }
            }
            else
            {
                var s = string.Format(
                    CultureInfo.InvariantCulture,
                    "UTC=[{0}] Level=[{1}] LogId=[{2}] Tag=[{3}] {4}",
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                    level,
                    logId,
                    tag,
                    format);

                if (level <= LoggerLevel.Warning)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                }

                if (objectParams == null || objectParams.Length == 0)
                {
                    Console.WriteLine(s);
                }
                else
                {
                    Console.WriteLine(s, objectParams);
                }

                Console.BackgroundColor = ConsoleColor.Black;
            }
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
        public bool IsLogged(LoggerLevel level, object logId, string tag)
        {
            return true;
        }

        /// <summary>
        /// Disposes resouces used by the object.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
