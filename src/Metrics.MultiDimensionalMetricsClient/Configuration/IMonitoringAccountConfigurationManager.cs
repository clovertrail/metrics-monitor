//-------------------------------------------------------------------------------------------------
// <copyright file="IMonitoringAccountConfigurationManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// This interface that manages monitoring account configurations.
    /// </summary>
    public interface IMonitoringAccountConfigurationManager
    {
        /// <summary>
        /// Get the monitoring account specified by the monitoring account name.
        /// </summary>
        /// <param name="monitoringAccountName">The name of the monitoring account.</param>
        /// <returns>The monitoring account.</returns>
        Task<IMonitoringAccount> GetAsync(string monitoringAccountName);

        /// <summary>
        /// Creates a monitoring account with provided configuration.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration.</param>
        /// <param name="stampHostName">The stamp name such as prod3.metrics.nsatc.net as documented @ https://aka.ms/mdm-endpoints.</param>
        /// <returns>A task the caller can wait on.</returns>
        Task CreateAsync(IMonitoringAccount monitoringAccount, string stampHostName);

        /// <summary>
        /// Create a new monitoring account named <paramref name="newMonitoringAccountName"/> on stamp <paramref name="stampHostName"/> by copying the common settings from <paramref name="monitoringAccountToCopyFrom" />.
        /// </summary>
        /// <param name="newMonitoringAccountName">The new monitoring account name.</param>
        /// <param name="monitoringAccountToCopyFrom">The name of the monitoring account where common settings are copied from.</param>
        /// <param name="stampHostName">The stamp name such as prod3.metrics.nsatc.net as documented @ https://aka.ms/mdm-endpoints.</param>
        /// <returns>A task the caller can wait on.</returns>
        Task CreateAsync(string newMonitoringAccountName, string monitoringAccountToCopyFrom, string stampHostName);

        /// <summary>
        /// Save the monitoring account configuration provided.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration to save.</param>
        /// <param name="skipVersionCheck">Flag indicating whether or not the version flag should be honored.</param>
        /// <returns>A task the caller can wait on.</returns>
        Task SaveAsync(IMonitoringAccount monitoringAccount, bool skipVersionCheck = false);

        /// <summary>
        /// Delete the monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration to delete.</param>
        /// <returns>A task the caller can wait on.</returns>
        Task DeleteAsync(string monitoringAccount);

        /// <summary>
        /// Un-Delete the monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration to un-delete.</param>
        /// <returns>A task the caller can wait on.</returns>
        Task UnDeleteAsync(string monitoringAccount);

        /// <summary>
        /// Synchronizes the monitoring account configuration asynchronous.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="skipVersionCheck">if set to <c>true</c> [skip version check].</param>
        /// <returns>A list of <see cref="ConfigurationUpdateResult"/>.</returns>
        Task<IReadOnlyList<ConfigurationUpdateResult>> SyncMonitoringAccountConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            bool skipVersionCheck = false);
    }
}