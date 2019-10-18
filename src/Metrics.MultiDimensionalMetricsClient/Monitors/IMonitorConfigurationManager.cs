// -----------------------------------------------------------------------
// <copyright file="IMonitorConfigurationManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Monitors
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Configuration;

    /// <summary>
    /// The interface of managing monitor configuration.
    /// </summary>
    internal interface IMonitorConfigurationManager
    {
        /// <summary>
        /// Sync all monitor configuration across mirror accounts.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="skipVersionCheck">True if skip version check.</param>
        /// <param name="validate">Whether to validate the monitor configuration in target accounts.</param>
        /// <returns>A list of configuration update result.</returns>
        Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            bool skipVersionCheck = false,
            bool validate = true);

        /// <summary>
        /// Sync all monitor configuration under specific namespace across mirror accounts.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="metricNamespace">Metric namespace.</param>
        /// <param name="skipVersionCheck">True if skip version check.</param>
        /// <param name="validate">Whether to validate the monitor configuration in target accounts.</param>
        /// <returns>A list of configuration update result.</returns>
        Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            bool skipVersionCheck = false,
            bool validate = true);

        /// <summary>
        /// Sync monitor configuration under specific metric across mirror accounts.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="metricNamespace">Metric namespace.</param>
        /// <param name="metricName">Metric name.</param>
        /// <param name="skipVersionCheck">True if skip version check.</param>
        /// <param name="validate">Whether to validate the monitor configuration in target accounts.</param>
        /// <returns>A list of configuration update result.</returns>
        Task<ConfigurationUpdateResultList> SyncConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            string metricName,
            bool skipVersionCheck = false,
            bool validate = true);
    }
}