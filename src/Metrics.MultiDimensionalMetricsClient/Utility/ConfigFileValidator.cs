//-------------------------------------------------------------------------------------------------
// <copyright file="ConfigFileValidator.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System.Linq;
    using Microsoft.Cloud.Metrics.Client.Configuration;
    using Microsoft.Cloud.Metrics.Client.Logging;

    /// <summary>
    /// The validator class for configuration files.
    /// </summary>
    internal static class ConfigFileValidator
    {
        private static readonly object LogId = Logger.CreateCustomLogId("ConfigFileValidator");

        /// <summary>
        /// Validates the raw metric configuration from file to see if they contain the expected configuration for the command in question.
        /// </summary>
        /// <param name="metricConfigFromFile">The metric configuration from file.</param>
        /// <returns>True if passing validation; false otherwise.</returns>
        internal static bool ValidateRawMetricConfigFromFile(IRawMetricConfiguration metricConfigFromFile)
        {
            if (metricConfigFromFile.Preaggregations == null || !metricConfigFromFile.Preaggregations.Any())
            {
                Logger.Log(LoggerLevel.Error, LogId, "ValidateRawMetricConfigFromFile", "Preaggregations property is not set or empty so it seems to be an invalid raw metric config.");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the composite metric configuration from file to see if they contain the expected configuration for the command in question.
        /// </summary>
        /// <param name="metricConfigFromFile">The metric configuration from file.</param>
        /// <returns>True if passing validation; false otherwise.</returns>
        internal static bool ValidateCompositeMetricConfigFromFile(ICompositeMetricConfiguration metricConfigFromFile)
        {
            if (metricConfigFromFile.MetricSources == null || !metricConfigFromFile.MetricSources.Any())
            {
                Logger.Log(LoggerLevel.Error, LogId, "ValidateCompositeMetricConfigFromFile", "MetricSources property is not set or empty so it seems to be an invalid composite metric config.");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the metric configuration from file to see if they contain the expected configuration contents.
        /// </summary>
        /// <param name="metricConfigFromFile">The metric configuration from file.</param>
        /// <returns>True if passing validation; false otherwise.</returns>
        internal static bool ValidateMetricConfigFromFile(IMetricConfiguration metricConfigFromFile)
        {
            return metricConfigFromFile is RawMetricConfiguration
                ? ValidateRawMetricConfigFromFile((RawMetricConfiguration)metricConfigFromFile)
                : ValidateCompositeMetricConfigFromFile((CompositeMetricConfiguration)metricConfigFromFile);
        }
    }
}