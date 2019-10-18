//-------------------------------------------------------------------------------------------------
// <copyright file="IMetricConfigurationManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// This interface that manages metric configurations.
    /// </summary>
    public interface IMetricConfigurationManager
    {
        /// <summary>
        /// Get the metric specified by the account, namespace and metric name.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">The metric name.</param>
        /// <returns>The metric.</returns>
        Task<IMetricConfiguration> GetAsync(IMonitoringAccount monitoringAccount, string metricNamespace, string metricName);

        /// <summary>
        /// Get the metric specified by the account, namespace and metric name.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="returnEmptyConfig">Determine if empty, unmodified configurations should be returned.</param>
        /// <returns>The metrics that match the criteria.</returns>
        Task<IReadOnlyList<IMetricConfiguration>> GetMultipleAsync(IMonitoringAccount monitoringAccount, string metricNamespace, bool returnEmptyConfig = false);

        /// <summary>
        /// Get the metric specified by the account, namespace and metric name.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="returnEmptyConfig">Determine if empty, unmodified configurations should be returned.</param>
        /// <returns>The metrics that match the criteria.</returns>
        Task<IReadOnlyList<IMetricConfiguration>> GetMultipleAsync(IMonitoringAccount monitoringAccount, bool returnEmptyConfig = false);

        /// <summary>
        /// Save the metric configuration provided.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration.</param>
        /// <param name="metricConfiguration">The metric to be saved.</param>
        /// <param name="skipVersionCheck">Flag indicating whether or not the version flag should be honored.</param>
        /// <returns>A task the caller can wait on.</returns>
        Task SaveAsync(IMonitoringAccount monitoringAccount, IMetricConfiguration metricConfiguration, bool skipVersionCheck = false);

        /// <summary>
        /// Deletes the metric configuration by metric name.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <returns>A task the caller can wait on.</returns>
        Task DeleteAsync(IMonitoringAccount monitoringAccount, string metricNamespace, string metricName);

        /// <summary>
        /// Sync all metric configuration across mirror accounts.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="skipVersionCheck">True if skip version check.</param>
        /// <returns>A list of configuration update result.</returns>
        Task<IReadOnlyList<IConfigurationUpdateResult>> SyncAllAsync(
            IMonitoringAccount monitoringAccount,
            bool skipVersionCheck = false);

        /// <summary>
        /// Sync all metric configuration across mirror accounts.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="metricNamespace">Metric namespace.</param>
        /// <param name="skipVersionCheck">True if skip version check.</param>
        /// <returns>A list of configuration update result.</returns>
        Task<IReadOnlyList<IConfigurationUpdateResult>> SyncAllAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            bool skipVersionCheck = false);

        /// <summary>
        /// Sync all metric and monitor configuration across mirror accounts.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="skipVersionCheck">True if skip version check.</param>
        /// <returns>A list of <see cref="ConfigurationUpdateResultList"/>.</returns>
        Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncAllAsyncV2(
            IMonitoringAccount monitoringAccount,
            bool skipVersionCheck = false);

        /// <summary>
        /// Sync all metric and monitor configuration across mirror accounts.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="metricNamespace">Metric namespace.</param>
        /// <param name="skipVersionCheck">True if skip version check.</param>
        /// <returns>A list of <see cref="ConfigurationUpdateResultList"/>.</returns>
        Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncAllAsyncV2(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            bool skipVersionCheck = false);

        /// <summary>
        /// Sync metric and monitor configuration for given metric name across mirror accounts.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="metricNamespace">Metric namespace.</param>
        /// <param name="metricName">Metric name.</param>
        /// <param name="skipVersionCheck">True if skip version check.</param>
        /// <returns>An instance of <see cref="ConfigurationUpdateResultList"/>.</returns>
        Task<ConfigurationUpdateResultList> SyncConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            string metricName,
            bool skipVersionCheck = false);

        /// <summary>
        /// Download metric configurations as json files.
        /// </summary>
        /// <param name="destinationFolder">folder for storing downloaded config json files.</param>
        /// <param name="monitoringAccount">Monitoring account</param>
        /// <param name="metricNamespace">metric namespace</param>
        /// <param name="metricName">metric name</param>
        /// <param name="metricNameRegex">metric name regex.</param>
        /// <param name="foldersOnNamespacesLevel">indicate if true, config files will be stored under corresponding namespaces folders.</param>
        /// <param name="downloadDefaultMetricConfig">indicate if true, default comfigs will also be downloaded.</param>
        /// <param name="maxFileNameProducedLength">max size of file name that will be created locally.</param>
        /// <returns> OperationStatus.</returns>
        Task<OperationStatus> DownloadMetricConfigurationAsync(
            string destinationFolder,
            IMonitoringAccount monitoringAccount,
            string metricNamespace = null,
            string metricName = null,
            Regex metricNameRegex = null,
            bool foldersOnNamespacesLevel = false,
            bool downloadDefaultMetricConfig = false,
            int maxFileNameProducedLength = 256);

        /// <summary>
        /// Modify local metric configuration json files by replacing AccountName.
        /// </summary>
        /// <param name="sourceFolder">folder in which config files locate.</param>
        /// <param name="monitoringAccount">Monitoring account</param>
        /// <param name="replaceAccountNameWith">account name to replace original account with in local config files.</param>
        /// <param name="metricNameRegex">metric name regex.</param>
        /// <returns> OperationStatus.</returns>
        Task<OperationStatus> ReplaceAccountNameInMetricConfigurationFilesAsync(
            string sourceFolder,
            IMonitoringAccount monitoringAccount,
            string replaceAccountNameWith,
            Regex metricNameRegex = null);

        /// <summary>
        /// Modify local metric configuration json files by replacing Namespace.
        /// </summary>
        /// <param name="sourceFolder">folder in which config files locate.</param>
        /// <param name="monitoringAccount">Monitoring account</param>
        /// <param name="replaceNamespaceWith">namespace to replace original namespace with in local config files.</param>
        /// <param name="metricNameRegex">metric name regex.</param>
        /// <returns> OperationStatus.</returns>
        Task<OperationStatus> ReplaceNamespaceInMetricConfigurationFilesAsync(
            string sourceFolder,
            IMonitoringAccount monitoringAccount,
            string replaceNamespaceWith,
            Regex metricNameRegex = null);

        /// <summary>
        /// Upload all metric configurations from the given folder to the same monitoring account.
        /// </summary>
        /// <param name="sourceFolder">folder in which config files locate.</param>
        /// <param name="monitoringAccount">Monitoring account</param>
        /// <param name="force">indicate if true, skip version check for metric config against existing metric config version.</param>
        /// <returns> OperationStatus.</returns>
        Task<OperationStatus> UploadMetricConfigurationAsync(
            string sourceFolder,
            IMonitoringAccount monitoringAccount,
            bool force = false);

        /// <summary>
        /// Apply metric configuration from local template file to many metrics under one monitoring account.
        /// </summary>
        /// <param name="templateFilePath">absolute path of template metric configuration json file.</param>
        /// <param name="monitoringAccount">Monitoring account</param>
        /// <param name="metricNamespace">metric namespace</param>
        /// <param name="metricName">metric name</param>
        /// <param name="metricNameRegex">metric name regex</param>
        /// <param name="force">indicate if true, will overwrite existing config on server and skip version check.</param>
        /// <param name="whatIf">indicate if true, show the template config without actually uploading the config</param>
        /// <returns> OperationStatus.</returns>
        Task<OperationStatus> ApplyTemplateMetricConfigurationAsync(
            string templateFilePath,
            IMonitoringAccount monitoringAccount,
            string metricNamespace = null,
            string metricName = null,
            Regex metricNameRegex = null,
            bool force = false,
            bool whatIf = false);

        /// <summary>
        /// Synchronizes all metrics configurations only asynchronously across mirror accounts.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="skipVersionCheck">if set to <c>true</c> [skip version check].</param>
        /// <returns>A list of <see cref="ConfigurationUpdateResultList"/>.</returns>
        Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncAllMetricsAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace = null,
            bool skipVersionCheck = false);

        /// <summary>
        /// Synchronizes the metric configuration only across mirror accounts asynchronously.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="skipVersionCheck">if set to <c>true</c> [skip version check].</param>
        /// <returns>An instance of <see cref="ConfigurationUpdateResultList"/>.</returns>
        Task<ConfigurationUpdateResultList> SyncMetricConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            string metricName,
            bool skipVersionCheck = false);
    }
}