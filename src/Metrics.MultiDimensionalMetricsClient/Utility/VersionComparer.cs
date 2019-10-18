//-------------------------------------------------------------------------------------------------
// <copyright file="VersionComparer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System.Linq;

    using Microsoft.Cloud.Metrics.Client.Configuration;
    using Microsoft.Cloud.Metrics.Client.Logging;

    using Newtonsoft.Json;

    /// <summary>
    /// The comparer class for configuration versions.
    /// </summary>
    internal static class VersionComparer
    {
        private static readonly object LogId = Logger.CreateCustomLogId("VersionComparer");

        /// <summary>
        /// Compares the metrics version with server.
        /// </summary>
        /// <param name="metricConfigFromFile">The metric configuration from file.</param>
        /// <param name="metricConfigurationOnServer">The metric configuration on server.</param>
        /// <returns>
        /// 1 if configuration from file has a higher version number, -1 if lower, and 0 if identical.
        /// </returns>
        internal static int CompareMetricsVersionWithServer(IMetricConfiguration metricConfigFromFile, IMetricConfiguration metricConfigurationOnServer)
        {
            const string logTag = "CompareMetricsVersionWithServer";
            if (metricConfigurationOnServer == null)
            {
                return 1;
            }

            if (metricConfigFromFile.Version == metricConfigurationOnServer.Version)
            {
                Logger.Log(
                    LoggerLevel.Warning,
                    LogId,
                    logTag,
                    "The version in the file is the same as the one on the server. Skip.");
                return 0;
            }

            if (metricConfigFromFile.Version < metricConfigurationOnServer.Version)
            {
                Logger.Log(
                    LoggerLevel.Warning,
                    LogId,
                    logTag,
                    $"The version {metricConfigFromFile.Version} in the file is less than the one {metricConfigurationOnServer.Version} on the server! Please download the latest version first. Skip.");

                return -1;
            }

            return 1;
        }
    }
}