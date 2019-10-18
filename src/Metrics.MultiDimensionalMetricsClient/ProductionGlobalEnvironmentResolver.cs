// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProductionGlobalEnvironmentResolver.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client
{
    using Microsoft.Cloud.Metrics.Client.Logging;

    using Newtonsoft.Json;

    /// <summary>
    /// A helper class to resolve the production global environment.
    /// </summary>
    /// <remarks>
    /// The LX project consists of two distinct clouds.  Neither will have connectivity to external networks and L3 hosted DNS names cannot be used.
    /// This impacts our current behavior of users not specifying a specific endpoint and us utilizing the fixed L3 hosted global stamp DNS to seed locating the appropriate owning stamp for a given account.
    /// The goal is to allow this behavior to continue while not requiring customers to modify their code and configuration when moving into LX,
    /// and this helper class tries to determine the production global environment by resolving the DNS names of a list of potential global environments and return the first succeeded one.
    /// </remarks>
    public class ProductionGlobalEnvironmentResolver
    {
        private static readonly object LogId = Logger.CreateCustomLogId("ProductionGlobalEnvironmentResolver");
        private static readonly string[] PotentialProductionGlobalEnvironments =
        {
            "global.metrics.nsatc.net",
            "global.metrics.trafficmanager.net",
        };

        private static string globalStampHostName;

        /// <summary>
        /// Gets the global stamp host name.
        /// </summary>
        /// <returns>The global stamp host name.</returns>
        public static string ResolveGlobalStampHostName()
        {
            if (globalStampHostName != null)
            {
                return globalStampHostName;
            }

            for (int i = 0; i < PotentialProductionGlobalEnvironments.Length; i++)
            {
                var resolvedIp = ConnectionInfo.ResolveIp(PotentialProductionGlobalEnvironments[i], throwOnFailure: false).GetAwaiter().GetResult();
                if (resolvedIp != null)
                {
                    globalStampHostName = PotentialProductionGlobalEnvironments[i];
                    return PotentialProductionGlobalEnvironments[i];
                }

                Logger.Log(LoggerLevel.Error, LogId, "ProductionGlobalEnvironmentResolver", $"Failed to resolve {PotentialProductionGlobalEnvironments[i]}.");
            }

            string errorMsg = $"ProductionGlobalEnvironmentResolver - None of the host names can be resolved: {JsonConvert.SerializeObject(PotentialProductionGlobalEnvironments)}.";
            Logger.Log(LoggerLevel.Error, LogId, "ProductionGlobalEnvironmentResolver", errorMsg);

            throw new MetricsClientException(errorMsg);
        }
    }
}
