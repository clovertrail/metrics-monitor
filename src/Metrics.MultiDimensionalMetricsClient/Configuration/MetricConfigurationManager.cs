// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricConfigurationManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Cloud.Metrics.Client.Logging;
    using Microsoft.Cloud.Metrics.Client.Metrics;
    using Microsoft.Cloud.Metrics.Client.Utility;
    using Monitors;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This class manages get and save operations on metric configurations.
    /// </summary>
    public sealed class MetricConfigurationManager : IMetricConfigurationManager
    {
        private static readonly object LogId = Logger.CreateCustomLogId("MetricConfigurationManager");
        private readonly ConnectionInfo connectionInfo;
        private readonly HttpClient httpClient;
        private readonly string metricConfigurationUrlPrefix;
        private readonly string metricUrlPrefix;
        private readonly JsonSerializerSettings serializerSettings;
        private readonly MonitorConfigurationManager monitorConfigManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricConfigurationManager"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information for the MDM endpoint being used.</param>
        public MetricConfigurationManager(ConnectionInfo connectionInfo)
            : this(connectionInfo, HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricConfigurationManager"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information for the MDM endpoint being used.</param>
        /// <param name="client">HttpClient for the connection to the MDM endpoint being used.</param>
        internal MetricConfigurationManager(ConnectionInfo connectionInfo, HttpClient client)
        {
            if (connectionInfo == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            this.connectionInfo = connectionInfo;
            this.metricConfigurationUrlPrefix = this.connectionInfo.GetAuthRelativeUrl("v1/config/metricConfiguration/");
            this.metricUrlPrefix = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.ConfigRelativeUrl);
            this.monitorConfigManager = new MonitorConfigurationManager(this.connectionInfo);

            this.httpClient = client;

            var migrations = new[]
            {
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.ComputedSamplingTypeExpressionImpl",
                    typeof(ComputedSamplingTypeExpression)),
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.PreaggregationImpl",
                    typeof(Preaggregation)),
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.MinMaxConfigurationImpl",
                    typeof(MinMaxConfiguration)),
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.PercentileConfigurationImpl",
                    typeof(PercentileConfiguration)),
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.RollupConfigurationImpl",
                    typeof(RollupConfiguration)),
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.PublicationConfigurationImpl",
                    typeof(PublicationConfiguration)),
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.DistinctCountConfigurationImpl",
                    typeof(DistinctCountConfiguration)),
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.RawMetricConfigurationImpl",
                    typeof(RawMetricConfiguration)),
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.CompositeMetricConfigurationImpl",
                    typeof(CompositeMetricConfiguration)),
                new ClientAssemblyMigration(
                    "Metrics.Server",
                    "Microsoft.Online.Metrics.Server.Utilities.ConfigurationUpdateResult",
                    typeof(ConfigurationUpdateResult)),
                new ClientAssemblyMigration(
                    "Microsoft.Online.Metrics.Common",
                    "Microsoft.Online.Metrics.Common.EventConfiguration.FilteringConfigurationImpl",
                    typeof(FilteringConfiguration)),
            };

            this.serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Binder = new ClientAssemblyMigrationSerializationBinder(migrations)
            };

            this.MaxParallelRunningTasks = 20;
        }

        /// <summary>
        /// Gets or sets the maximum parallel running tasks.
        /// </summary
        public int MaxParallelRunningTasks { get; set; }

        /// <summary>
        /// Get the metric specified by the account, namespace and metric name.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">The metric name.</param>
        /// <returns>The metric.</returns>
        public async Task<IMetricConfiguration> GetAsync(IMonitoringAccount monitoringAccount, string metricNamespace, string metricName)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrWhiteSpace(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            if (string.IsNullOrWhiteSpace(metricName))
            {
                throw new ArgumentNullException(nameof(metricName));
            }

            var path = $"{this.metricConfigurationUrlPrefix}/monitoringAccount/{monitoringAccount.Name}/metricNamespace/{SpecialCharsHelper.EscapeTwice(metricNamespace)}/metric/{SpecialCharsHelper.EscapeTwice(metricName)}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount.Name))
            {
                Path = path
            };

            try
            {
                var response = await HttpClientHelper.GetResponse(
                    uriBuilder.Uri,
                    HttpMethod.Get,
                    this.httpClient,
                    monitoringAccount.Name,
                    this.metricConfigurationUrlPrefix).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<IMetricConfiguration[]>(response.Item1, this.serializerSettings)[0];
            }
            catch (MetricsClientException mce)
            {
                if (mce.ResponseStatusCode.HasValue && mce.ResponseStatusCode.Value == HttpStatusCode.NotFound)
                {
                    throw new MetricNotFoundException($"Metric [{monitoringAccount.Name}][{metricNamespace}][{metricName}] not found. TraceId: [{mce.TraceId}]", mce);
                }

                throw;
            }
        }

        /// <summary>
        /// Get the metric specified by the account, namespace and metric name.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="returnEmptyConfig">Determine if empty, unmodified configurations should be returned.</param>
        /// <returns>The metrics that match the criteria.</returns>
        public async Task<IReadOnlyList<IMetricConfiguration>> GetMultipleAsync(IMonitoringAccount monitoringAccount, string metricNamespace, bool returnEmptyConfig = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrWhiteSpace(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            var path = $"{this.metricConfigurationUrlPrefix}/monitoringAccount/{monitoringAccount.Name}/metricNamespace/{SpecialCharsHelper.EscapeTwice(metricNamespace)}";
            var query = $"includeEmptyConfig={returnEmptyConfig}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount.Name))
            {
                Path = path,
                Query = query
            };

            try
            {
                var response = await HttpClientHelper.GetResponse(
                    uriBuilder.Uri,
                    HttpMethod.Get,
                    this.httpClient,
                    monitoringAccount.Name,
                    this.metricConfigurationUrlPrefix).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<IMetricConfiguration[]>(response.Item1, this.serializerSettings);
            }
            catch (MetricsClientException mce)
            {
                if (mce.ResponseStatusCode.HasValue && mce.ResponseStatusCode.Value == HttpStatusCode.NotFound)
                {
                    throw new MetricNotFoundException($"Metrics under [{monitoringAccount.Name}][{metricNamespace}] not found. TraceId: [{mce.TraceId}]", mce);
                }

                throw;
            }
        }

        /// <summary>
        /// Get the metric specified by the account, namespace and metric name.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="returnEmptyConfig">Determine if empty, unmodified configurations should be returned.</param>
        /// <returns>The metrics that match the criteria.</returns>
        public async Task<IReadOnlyList<IMetricConfiguration>> GetMultipleAsync(IMonitoringAccount monitoringAccount, bool returnEmptyConfig = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            var path = $"{this.metricConfigurationUrlPrefix}/monitoringAccount/{monitoringAccount.Name}";
            var query = $"includeEmptyConfig={returnEmptyConfig}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount.Name))
            {
                Path = path,
                Query = query
            };

            try
            {
                var response = await HttpClientHelper.GetResponse(
                    uriBuilder.Uri,
                    HttpMethod.Get,
                    this.httpClient,
                    monitoringAccount.Name,
                    this.metricConfigurationUrlPrefix).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<IMetricConfiguration[]>(response.Item1, this.serializerSettings);
            }
            catch (MetricsClientException mce)
            {
                if (mce.ResponseStatusCode.HasValue && mce.ResponseStatusCode.Value == HttpStatusCode.NotFound)
                {
                    throw new MetricNotFoundException($"Metrics under [{monitoringAccount.Name}] not found. TraceId: [{mce.TraceId}]", mce);
                }

                throw;
            }
        }

        /// <summary>
        /// Save the metric configuration provided.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration.</param>
        /// <param name="metricConfiguration">The metric to be saved.</param>
        /// <param name="skipVersionCheck">Flag indicating whether or not the version flag should be honored.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task SaveAsync(IMonitoringAccount monitoringAccount, IMetricConfiguration metricConfiguration, bool skipVersionCheck = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (metricConfiguration == null)
            {
                throw new ArgumentNullException(nameof(metricConfiguration));
            }

            var path = $"{this.metricConfigurationUrlPrefix}/monitoringAccount/{monitoringAccount.Name}/metricNamespace/{SpecialCharsHelper.EscapeTwice(metricConfiguration.MetricNamespace)}/metric/{SpecialCharsHelper.EscapeTwice(metricConfiguration.Name)}/skipVersionCheck/{skipVersionCheck}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount.Name))
            {
                Path = path,
                Query = "apiVersion=1"
            };

            var serializedMetric = JsonConvert.SerializeObject(new[] { metricConfiguration }, Formatting.Indented, this.serializerSettings);

            try
            {
                await HttpClientHelper.GetResponse(
                    uriBuilder.Uri,
                    HttpMethod.Post,
                    this.httpClient,
                    monitoringAccount.Name,
                    this.metricConfigurationUrlPrefix,
                    httpContent: serializedMetric).ConfigureAwait(false);
            }
            catch (MetricsClientException mce)
            {
                if (mce.ResponseStatusCode == HttpStatusCode.BadRequest)
                {
                    throw new ConfigurationValidationException(
                        $"Metric [{monitoringAccount.Name}][{metricConfiguration.MetricNamespace}][{metricConfiguration.Name}] could not be saved because validation failed. Response: {mce.Message}",
                        ValidationType.ServerSide,
                        mce);
                }

                throw;
            }
        }

        /// <summary>
        /// Deletes the metric configuration by metric name.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task DeleteAsync(IMonitoringAccount monitoringAccount, string metricNamespace, string metricName)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrWhiteSpace(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            if (string.IsNullOrWhiteSpace(metricName))
            {
                throw new ArgumentNullException(nameof(metricName));
            }

            var operation = $"{this.connectionInfo.GetAuthRelativeUrl(string.Empty)}v1/config/metrics";

            var path =
                $"{operation}/monitoringAccount/{monitoringAccount.Name}/metricNamespace/{SpecialCharsHelper.EscapeTwice(metricNamespace)}/metric/{SpecialCharsHelper.EscapeTwice(metricName)}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount.Name))
            {
                Path = path
            };

            await HttpClientHelper.GetResponse(
                uriBuilder.Uri,
                HttpMethod.Delete,
                this.httpClient,
                monitoringAccount.Name,
                operation).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IConfigurationUpdateResult>> SyncAllAsync(
            IMonitoringAccount monitoringAccount,
            bool skipVersionCheck = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            var metricReader = new MetricReader(this.connectionInfo);

            var namespaces =
                await metricReader.GetNamespacesAsync(monitoringAccount.Name).ConfigureAwait(false);

            IReadOnlyList<IConfigurationUpdateResult> results = null;
            List<string> namespaceswithTimeout = new List<string>();
            foreach (var ns in namespaces)
            {
                try
                {
                    var namespaceResults =
                        await this.SyncAllAsync(monitoringAccount, ns, skipVersionCheck)
                            .ConfigureAwait(false);

                    if (namespaceResults.Any(updateResult => !updateResult.Success))
                    {
                        return namespaceResults;
                    }

                    // For QOS namespaces or other internal namespaces, there is no configuration to
                    // replicate and thus the namespace results is an empty list.
                    if (namespaceResults.Count > 0)
                    {
                        results = namespaceResults;
                    }
                }
                catch (MetricsClientException mce)
                {
                    if (!mce.ResponseStatusCode.HasValue || mce.ResponseStatusCode == HttpStatusCode.GatewayTimeout)
                    {
                        namespaceswithTimeout.Add(ns);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (namespaceswithTimeout.Count > 0)
            {
                var msg =
                    $"Failed to sync all configurations for namespaces:{string.Join(",", namespaceswithTimeout)}."
                    + " Please try again for these namespaces.";

                throw new MetricsClientException(msg);
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IConfigurationUpdateResult>> SyncAllAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            bool skipVersionCheck = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrEmpty(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            var operation = $"{this.metricUrlPrefix}/replicateConfigurationToMirrorAccounts";
            var path =
                $"{operation}/monitoringAccount/{monitoringAccount.Name}/metricNamespace/{SpecialCharsHelper.EscapeTwice(metricNamespace)}/skipVersionCheck/{skipVersionCheck}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetGlobalEndpoint())
                                 {
                                     Path = path
                                 };

            try
            {
                var response = await HttpClientHelper.GetResponse(
                                   uriBuilder.Uri,
                                   HttpMethod.Post,
                                   this.httpClient,
                                   monitoringAccount.Name,
                                   operation).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<ConfigurationUpdateResult[]>(
                    response.Item1,
                    this.serializerSettings);
            }
            catch (MetricsClientException mce)
            {
                if (mce.ResponseStatusCode == HttpStatusCode.BadRequest)
                {
                    var exMsg = $"Unable to sync all configuration for metric namespace : {metricNamespace} as either "
                        + $"no mirror accounts found for monitoring account : {monitoringAccount.Name} or user doesn't "
                        + $"have permission to update configurations in mirror accounts. Response : {mce.Message}";

                    throw new ConfigurationValidationException(exMsg, ValidationType.ServerSide, mce);
                }

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncAllAsyncV2(
            IMonitoringAccount monitoringAccount,
            bool skipVersionCheck = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            var metricReader = new MetricReader(this.connectionInfo);

            var namespaces =
                await metricReader.GetNamespacesAsync(monitoringAccount.Name).ConfigureAwait(false);

            List<ConfigurationUpdateResultList> results = new List<ConfigurationUpdateResultList>();
            foreach (var ns in namespaces)
            {
                var namespaceResults = await this.SyncAllAsyncV2(
                        monitoringAccount,
                        ns,
                        skipVersionCheck).ConfigureAwait(false);

                // For QOS namespaces or other internal namespaces, there is no configuration to
                // replicate and thus the namespace results is an empty list.
                if (namespaceResults.Count > 0)
                {
                    results.AddRange(namespaceResults);
                }
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncAllAsyncV2(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            bool skipVersionCheck = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrEmpty(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            var metricReader = new MetricReader(this.connectionInfo);
            var metricNames = await metricReader.GetMetricNamesAsync(
                    monitoringAccount.Name,
                    metricNamespace).ConfigureAwait(false);
            var taskList = new List<Task<ConfigurationUpdateResultList>>(this.MaxParallelRunningTasks);

            List<ConfigurationUpdateResultList> results = new List<ConfigurationUpdateResultList>();
            foreach (var metricName in metricNames)
            {
                if (taskList.Count == this.MaxParallelRunningTasks)
                {
                    await this.WaitAllForSyncAllAsyncV2(taskList, results).ConfigureAwait(false);
                    taskList.Clear();
                }

                taskList.Add(this.SyncConfigurationAsync(monitoringAccount, metricNamespace, metricName, skipVersionCheck));
            }

            if (taskList.Count > 0)
            {
                await this.WaitAllForSyncAllAsyncV2(taskList, results).ConfigureAwait(false);
                taskList.Clear();
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<ConfigurationUpdateResultList> SyncConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            string metricName,
            bool skipVersionCheck = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrWhiteSpace(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            if (string.IsNullOrWhiteSpace(metricName))
            {
                throw new ArgumentNullException(nameof(metricName));
            }

            var operation = $"{this.metricUrlPrefix}/replicateConfigurationToMirrorAccounts";
            var path =
                $"{operation}/monitoringAccount/{monitoringAccount.Name}/metricNamespace/{SpecialCharsHelper.EscapeTwice(metricNamespace)}/skipVersionCheck/{skipVersionCheck}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetGlobalEndpoint())
            {
                Path = path,
                Query = $"metricName={metricName}"
            };

            var result = new ConfigurationUpdateResultList
            {
                MonitoringAccount = monitoringAccount.Name,
                MetricNamespace = metricNamespace,
                MetricName = metricName
            };

            try
            {
                var response = await HttpClientHelper.GetResponse(
                    uriBuilder.Uri,
                    HttpMethod.Post,
                    this.httpClient,
                    monitoringAccount.Name,
                    operation).ConfigureAwait(false);

                result.ConfigurationUpdateResults =
                    JsonConvert.DeserializeObject<ConfigurationUpdateResult[]>(
                        response.Item1,
                        this.serializerSettings);

                foreach (var updateResult in result.ConfigurationUpdateResults)
                {
                    if (!updateResult.Success)
                    {
                        result.Success = false;
                        result.ExceptionMessage = updateResult.Message;
                        return result;
                    }
                }

                // Sync monitor configurations
                var monitorConfigurationUpdatedResult = await this.monitorConfigManager.SyncConfigurationAsync(
                        monitoringAccount,
                        metricNamespace,
                        metricName,
                        skipVersionCheck)
                    .ConfigureAwait(false);

                if (monitorConfigurationUpdatedResult.ConfigurationUpdateResults == null || !monitorConfigurationUpdatedResult.ConfigurationUpdateResults.Any())
                {
                    result.Success = false;
                    return result;
                }

                foreach (var updateResult in monitorConfigurationUpdatedResult.ConfigurationUpdateResults)
                {
                    if (!updateResult.Success)
                    {
                        result.Success = false;
                        result.ExceptionMessage = updateResult.Message;
                        return result;
                    }
                }

                result.Success = true;
                return result;
            }
            catch (MetricsClientException mce)
            {
                result.Success = false;

                if (mce.ResponseStatusCode == HttpStatusCode.Unauthorized || mce.ResponseStatusCode == HttpStatusCode.Forbidden)
                {
                    var exMsg =
                        $"Unable to sync configuration for monitoringAccount:{monitoringAccount.Name}, metricNamespace:"
                        + $"{metricNamespace}, metricName:{metricName} as user"
                        + $"doesn't have permission to update configurations in mirror accounts. Response:{mce.Message}";

                    throw new ConfigurationValidationException(exMsg, ValidationType.ServerSide, mce);
                }
                else
                {
                    result.ExceptionMessage = mce.Message;
                }

                return result;
            }
        }

        /// <inheritdoc />
        public async Task<OperationStatus> DownloadMetricConfigurationAsync(
            string destinationFolder,
            IMonitoringAccount monitoringAccount,
            string metricNamespace = null,
            string metricName = null,
            Regex metricNameRegex = null,
            bool foldersOnNamespacesLevel = false,
            bool downloadDefaultMetricConfig = false,
            int maxFileNameProducedLength = 256)
        {
            const string logTag = "DownloadMetricConfigurationAsync";
            Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Target folder to save configurations is {destinationFolder}.");

            if (!FileOperationHelper.CreateFolderIfNotExists(destinationFolder))
            {
                Logger.Log(LoggerLevel.Error, LogId, logTag, $"Cannot create folder {destinationFolder} on local disk.");
                return OperationStatus.FolderCreationError;
            }

            var reader = new MetricReader(this.connectionInfo);
            IReadOnlyList<string> namespaces = string.IsNullOrWhiteSpace(metricNamespace)
                ? await reader.GetNamespacesAsync(monitoringAccount.Name).ConfigureAwait(false)
                : new[] { metricNamespace };

            if (namespaces == null || namespaces.Count == 0)
            {
                Logger.Log(LoggerLevel.Warning, LogId, logTag, $"No namespace is found under {monitoringAccount.Name}!");
                return OperationStatus.ResourceNotFound;
            }

            var operationResult = OperationStatus.CompleteSuccess;

            var getMetricsTaskList = new List<Task<IMetricConfiguration>>();

            // Metrics counters for better statistics
            int totalMetricsCount = 0;
            int retrievedMetricsCount = 0;
            int skippedMetricsCount = 0;

            foreach (var currentNamespace in namespaces)
            {
                var currentFolder = destinationFolder;

                if (foldersOnNamespacesLevel)
                {
                    var subFolder = FileNamePathHelper.ConvertPathToValidFolderName(currentNamespace);
                    currentFolder += Path.DirectorySeparatorChar + subFolder;
                    if (!FileOperationHelper.CreateFolderIfNotExists(currentFolder))
                    {
                        Logger.Log(LoggerLevel.Error, LogId, logTag, $"Cannot create folder {currentFolder} on local disk.");
                        return OperationStatus.FolderCreationError;
                    }
                }

                var metricNames = string.IsNullOrWhiteSpace(metricName)
                    ? await reader.GetMetricNamesAsync(monitoringAccount.Name, currentNamespace).ConfigureAwait(false)
                    : new[] { metricName };

                if (metricNames == null || metricNames.Count == 0)
                {
                    Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"No metric name found under {currentNamespace}.");
                    continue;
                }

                using (var throttler = new SemaphoreSlim(this.MaxParallelRunningTasks))
                {
                    foreach (var currentMetric in metricNames)
                    {
                        if (metricNameRegex != null && !metricNameRegex.IsMatch(currentMetric))
                        {
                            continue;
                        }

                        await throttler.WaitAsync().ConfigureAwait(false);
                        getMetricsTaskList.Add(Task.Run(async () =>
                        {
                            try
                            {
                                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Getting metric {currentMetric} in namespace {currentNamespace} ...");
                                return await this.GetAsync(monitoringAccount, currentNamespace, currentMetric)
                                    .ConfigureAwait(false);
                            }
                            finally
                            {
                                throttler.Release();
                            }
                        }));

                        totalMetricsCount++;
                    }

                    try
                    {
                        await Task.WhenAll(getMetricsTaskList).ConfigureAwait(false);
                    }
                    catch
                    {
                        var getExceptionsTaskList = getMetricsTaskList.Where(t => t.Exception != null);
                        foreach (var exTask in getExceptionsTaskList)
                        {
                            Logger.Log(LoggerLevel.Error, LogId, logTag, $"GetMetricsTasks Exception thrown : {exTask.Exception.Flatten()}");
                        }

                        operationResult = OperationStatus.ResourceGetError;
                    }
                }

                foreach (var successTask in getMetricsTaskList.Where(t => t.Exception == null))
                {
                    // Calculate count of successfully retrieved metrics for processing
                    retrievedMetricsCount++;

                    var downloadedMetric = successTask.Result;
                    var processResult = this.ProcessRetrievedMetrics(
                        downloadedMetric,
                        monitoringAccount.Name,
                        downloadDefaultMetricConfig,
                        maxFileNameProducedLength,
                        currentFolder);

                    // Update current non-error result if any error results returned
                    if ((operationResult == OperationStatus.CompleteSuccess || operationResult == OperationStatus.ResourceSkipped)
                        && processResult != OperationStatus.CompleteSuccess)
                    {
                        operationResult = processResult;
                    }

                    if (processResult == OperationStatus.ResourceSkipped)
                    {
                        skippedMetricsCount++;
                    }
                }

                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Metrics under namespace {currentNamespace} are processed.");

                getMetricsTaskList.Clear();
            }

            Logger.Log(
                LoggerLevel.CustomerFacingInfo,
                LogId,
                logTag,
                $"Detail statistics : For account {monitoringAccount.Name} , totally {totalMetricsCount} metrics are requested, {retrievedMetricsCount} metrics configuration are retrieved, {retrievedMetricsCount - skippedMetricsCount} metrics are saved as files.");

            if (totalMetricsCount == 0 || retrievedMetricsCount == skippedMetricsCount)
            {
                return OperationStatus.ResourceNotFound;
            }

            return operationResult;
        }

        /// <inheritdoc />
        public async Task<OperationStatus> ReplaceAccountNameInMetricConfigurationFilesAsync(
            string sourceFolder,
            IMonitoringAccount monitoringAccount,
            string replaceAccountNameWith,
            Regex metricNameRegex = null)
        {
            return await this.ModifyMetricConfigurationFilesAsync(
                sourceFolder,
                monitoringAccount,
                metricNameRegex,
                replaceAccountNameWith,
                replaceNamespaceWith: null).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<OperationStatus> ReplaceNamespaceInMetricConfigurationFilesAsync(
            string sourceFolder,
            IMonitoringAccount monitoringAccount,
            string replaceNamespaceWith,
            Regex metricNameRegex = null)
        {
            return await this.ModifyMetricConfigurationFilesAsync(
                sourceFolder,
                monitoringAccount,
                metricNameRegex,
                replaceAccountNameWith: null,
                replaceNamespaceWith: replaceNamespaceWith).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<OperationStatus> UploadMetricConfigurationAsync(
            string sourceFolder,
            IMonitoringAccount monitoringAccount,
            bool force = false)
        {
            const string logTag = "UploadMetricConfigurationAsync";
            const string jsonFileExtension = "*.json";
            Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Folder to read is {sourceFolder}");

            var operationResult = OperationStatus.CompleteSuccess;
            var uploadTaskList = new List<Task>();

            var totalFilesCount = 0;
            var failedFilesCount = 0;

            if (!force)
            {
                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, "Version check is enabled, Server will increment uploaded metric configuration version by 1.");
            }

            using (var throttler = new SemaphoreSlim(this.MaxParallelRunningTasks))
            {
                foreach (var currentConfigFile in Directory.EnumerateFiles(sourceFolder, jsonFileExtension))
                {
                    IMetricConfiguration metricConfigFromFile;
                    try
                    {
                        metricConfigFromFile = this.ReadFileAsMetricConfiguration(currentConfigFile);
                        if (!ConfigFileValidator.ValidateMetricConfigFromFile(metricConfigFromFile))
                        {
                            Logger.Log(LoggerLevel.Error, LogId, logTag, $"Metric Config file {currentConfigFile} failed validation.");
                            operationResult = OperationStatus.FileCorrupted;
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        operationResult = OperationStatus.FileCorrupted;
                        continue;
                    }

                    totalFilesCount++;

                    await throttler.WaitAsync().ConfigureAwait(false);
                    uploadTaskList.Add(Task.Run(async () =>
                    {
                        try
                        {
                            Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Uploading metric configuration from config file {currentConfigFile}");
                            await this.SaveAsync(monitoringAccount, metricConfigFromFile, force);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }

                try
                {
                    await Task.WhenAll(uploadTaskList).ConfigureAwait(false);
                }
                catch
                {
                    var exceptionTaskList = uploadTaskList.Where(t => t.Exception != null);
                    foreach (var exTask in exceptionTaskList)
                    {
                        Logger.Log(LoggerLevel.Error, LogId, logTag, $"Upload Task throw Exception : {exTask.Exception.Flatten()}");
                        failedFilesCount++;
                    }

                    operationResult = OperationStatus.ResourcePostError;
                }
            }

            Logger.Log(
                LoggerLevel.CustomerFacingInfo,
                LogId,
                logTag,
                $"Detail statistics : Total {totalFilesCount} config files are correctly parsed and pending for upload. {totalFilesCount - failedFilesCount} configs are uploaded successfully.");
            return operationResult;
        }

        /// <inheritdoc />
        public async Task<OperationStatus> ApplyTemplateMetricConfigurationAsync(
            string templateFilePath,
            IMonitoringAccount monitoringAccount,
            string metricNamespace = null,
            string metricName = null,
            Regex metricNameRegex = null,
            bool force = false,
            bool whatIf = false)
        {
            const string logTag = "ApplyTemplateMetricConfigurationAsync";
            Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"The template file path is {templateFilePath}.");

            IMetricConfiguration metricConfigTemplate;
            try
            {
                metricConfigTemplate = this.ReadFileAsMetricConfiguration(templateFilePath);
                if (!ConfigFileValidator.ValidateMetricConfigFromFile(metricConfigTemplate))
                {
                    Logger.Log(LoggerLevel.Error, LogId, logTag, $"Template file {templateFilePath} failed validation.");
                    return OperationStatus.FileCorrupted;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LoggerLevel.Error, LogId, logTag, $"Template file {templateFilePath} is corrupted for parsing. Exception: {ex}");
                return OperationStatus.FileCorrupted;
            }

            if (whatIf)
            {
                var templateString = JsonConvert.SerializeObject(metricConfigTemplate, Formatting.Indented, this.serializerSettings);
                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"The template configuration to apply is :\n{templateString}");
                return OperationStatus.CompleteSuccess;
            }

            var reader = new MetricReader(this.connectionInfo);
            IReadOnlyList<string> namespaces = string.IsNullOrWhiteSpace(metricNamespace)
                ? await reader.GetNamespacesAsync(monitoringAccount.Name).ConfigureAwait(false)
                : new[] { metricNamespace };

            if (namespaces == null || namespaces.Count == 0)
            {
                Logger.Log(LoggerLevel.Warning, LogId, logTag, $"No namespace is found under {monitoringAccount.Name}!");
                return OperationStatus.ResourceNotFound;
            }

            var operationResult = OperationStatus.CompleteSuccess;
            var uploadTaskList = new List<Task>();

            var totalMetricsCount = 0;
            var skippedMetricsCount = 0;
            var failedMetricsCount = 0;
            var metricsToApplyCount = 0;

            using (var throttler = new SemaphoreSlim(this.MaxParallelRunningTasks))
            {
                foreach (var currentNamespace in namespaces)
                {
                    var metricNames = string.IsNullOrWhiteSpace(metricName)
                        ? await reader.GetMetricNamesAsync(monitoringAccount.Name, currentNamespace).ConfigureAwait(false)
                        : new[] { metricName };

                    if (metricNames == null || metricNames.Count == 0)
                    {
                        Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"No metric name found under {currentNamespace}.");
                        continue;
                    }

                    foreach (var currentMetric in metricNames)
                    {
                        if (metricNameRegex != null && !metricNameRegex.IsMatch(currentMetric))
                        {
                            continue;
                        }

                        totalMetricsCount++;
                        if (!force)
                        {
                            try
                            {
                                var metricOnServer = await this.GetAsync(monitoringAccount, metricNamespace, metricName).ConfigureAwait(false);
                                bool isDefaultMetric = this.IsDefaultMetric(metricOnServer);

                                // Apply template will not overwrite existing configuration on server unless force option is enabled.
                                if (!isDefaultMetric)
                                {
                                    Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, "Existing metric configuration is detected on server. Skip applying template.");
                                    skippedMetricsCount++;
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Exception getting metric configuration from server. Skip applying template. Exceptions: {ex}");
                                continue;
                            }
                        }

                        var metricConfigToUpload = this.ApplyTemplateConfigWithDifferentMetric(metricConfigTemplate, currentNamespace, currentMetric);

                        await throttler.WaitAsync().ConfigureAwait(false);
                        uploadTaskList.Add(Task.Run(async () =>
                        {
                            try
                            {
                                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Uploading template metric configuration to Metric [{monitoringAccount.Name}][{currentNamespace}][{currentMetric}]");
                                await this.SaveAsync(monitoringAccount, metricConfigToUpload, force);
                            }
                            finally
                            {
                                throttler.Release();
                            }
                        }));
                    }
                }

                try
                {
                    metricsToApplyCount = uploadTaskList.Count;
                    await Task.WhenAll(uploadTaskList).ConfigureAwait(false);
                }
                catch
                {
                    var exceptionTaskList = uploadTaskList.Where(t => t.Exception != null);
                    foreach (var exTask in exceptionTaskList)
                    {
                        Logger.Log(LoggerLevel.Error, LogId, logTag, $"Apply Template upload task throw Exception : {exTask.Exception.Flatten()}");
                        failedMetricsCount++;
                    }

                    operationResult = OperationStatus.ResourcePostError;
                }
            }

            Logger.Log(
                LoggerLevel.CustomerFacingInfo,
                LogId,
                logTag,
                $"Detail statistics : {metricsToApplyCount - failedMetricsCount} metrics are successfully applied.\nTotal {totalMetricsCount} matching metrics are requested for applying template. {skippedMetricsCount} metrics are skipped due to already existence on server.");
            if (totalMetricsCount == 0 || skippedMetricsCount == totalMetricsCount)
            {
                return OperationStatus.ResourceNotFound;
            }

            return operationResult;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncAllMetricsAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace = null,
            bool skipVersionCheck = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            var metricReader = new MetricReader(this.connectionInfo);
            IReadOnlyList<string> namespaces;
            if (string.IsNullOrWhiteSpace(metricNamespace))
            {
                namespaces =
                    await metricReader.GetNamespacesAsync(monitoringAccount.Name).ConfigureAwait(false);
            }
            else
            {
                namespaces = new List<string> { metricNamespace };
            }

            var taskList = new List<Task<ConfigurationUpdateResultList>>(this.MaxParallelRunningTasks);
            var results = new List<ConfigurationUpdateResultList>();
            foreach (var ns in namespaces)
            {
                IReadOnlyList<string> metricNames =
                    await metricReader.GetMetricNamesAsync(
                        monitoringAccount.Name,
                        ns).ConfigureAwait(false);

                foreach (var metricName in metricNames)
                {
                    taskList.Add(
                        this.SyncMetricConfigurationAsync(
                            monitoringAccount,
                            ns,
                            metricName,
                            skipVersionCheck));

                    if (taskList.Count == this.MaxParallelRunningTasks)
                    {
                        await this.WaitAllForSyncAllAsyncV2(taskList, results).ConfigureAwait(false);
                        taskList.Clear();
                    }
                }
            }

            if (taskList.Count > 0)
            {
                await this.WaitAllForSyncAllAsyncV2(taskList, results).ConfigureAwait(false);
                taskList.Clear();
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<ConfigurationUpdateResultList> SyncMetricConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            string metricName,
            bool skipVersionCheck = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrWhiteSpace(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            if (string.IsNullOrWhiteSpace(metricName))
            {
                throw new ArgumentNullException(nameof(metricName));
            }

            var operation = $"{this.metricUrlPrefix}/replicateMetricConfigurationToMirrorAccounts";
            var path =
                $"{operation}/monitoringAccount/{monitoringAccount.Name}/metricNamespace/{SpecialCharsHelper.EscapeTwice(metricNamespace)}";
            var uriBuilder = new UriBuilder(this.connectionInfo.GetGlobalEndpoint())
            {
                Path = path,
                Query = $"metricName={metricName}&skipVersionCheck={skipVersionCheck}"
            };

            var result = new ConfigurationUpdateResultList
            {
                MonitoringAccount = monitoringAccount.Name,
                MetricNamespace = metricNamespace,
                MetricName = metricName
            };

            try
            {
                var response = await HttpClientHelper.GetResponse(
                    uriBuilder.Uri,
                    HttpMethod.Post,
                    this.httpClient,
                    monitoringAccount.Name,
                    operation).ConfigureAwait(false);

                result.ConfigurationUpdateResults =
                    JsonConvert.DeserializeObject<ConfigurationUpdateResult[]>(
                        response.Item1,
                        this.serializerSettings);

                foreach (var updateResult in result.ConfigurationUpdateResults)
                {
                    if (!updateResult.Success)
                    {
                        result.Success = false;
                        result.ExceptionMessage = updateResult.Message;
                        return result;
                    }
                }

                result.Success = true;
                return result;
            }
            catch (MetricsClientException mce)
            {
                result.Success = false;

                if (mce.ResponseStatusCode == HttpStatusCode.Unauthorized)
                {
                    var exMsg =
                        $"Unable to sync configuration for monitoringAccount:{monitoringAccount.Name}, metricNamespace:"
                        + $"{metricNamespace}, metricName:{metricName} as user"
                        + $"doesn't have permission to update configurations in mirror accounts. Response:{mce.Message}";

                    throw new ConfigurationValidationException(exMsg, ValidationType.ServerSide, mce);
                }
                else
                {
                    result.ExceptionMessage = mce.Message;
                }

                return result;
            }
        }

        /// <summary>
        /// Modify local metric configuration json files by replacing AccountName.
        /// </summary>
        /// <param name="sourceFolder">folder in which config files locate.</param>
        /// <param name="monitoringAccount">Monitoring account</param>
        /// <param name="metricNameRegex">metric name regex.</param>
        /// <param name="replaceAccountNameWith">account name to replace original account with in local config files.</param>
        /// <param name="replaceNamespaceWith">namespace to replace original namespace with in local config files.</param>
        /// <returns> OperationStatus.</returns>
        private async Task<OperationStatus> ModifyMetricConfigurationFilesAsync(
            string sourceFolder,
            IMonitoringAccount monitoringAccount,
            Regex metricNameRegex = null,
            string replaceAccountNameWith = null,
            string replaceNamespaceWith = null)
        {
            const string logTag = "ModifyMetricConfigurationFiles";
            const string jsonFileExtension = "*.json";
            Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Folder to read is {sourceFolder}");

            var operationResult = OperationStatus.CompleteSuccess;
            var modifyConfigTaskList = new List<Task>();

            var totalFilesCount = 0;
            var failedFilesCount = 0;

            foreach (var currentConfigFile in Directory.EnumerateFiles(sourceFolder, jsonFileExtension))
            {
                IMetricConfiguration metricConfigFromFile;
                try
                {
                    metricConfigFromFile = this.ReadFileAsMetricConfiguration(currentConfigFile);
                    if (metricNameRegex != null && !metricNameRegex.IsMatch(metricConfigFromFile.Name))
                    {
                        continue;
                    }
                }
                catch (Exception)
                {
                    operationResult = OperationStatus.FileCorrupted;
                    continue;
                }

                totalFilesCount++;
                modifyConfigTaskList.Add(
                    this.ModifyMetricConfigurationAsync(
                        currentConfigFile,
                        metricConfigFromFile,
                        monitoringAccount,
                        replaceAccountNameWith,
                        replaceNamespaceWith));
            }

            try
            {
                await Task.WhenAll(modifyConfigTaskList).ConfigureAwait(false);
            }
            catch
            {
                var modifyExceptionsTaskList = modifyConfigTaskList.Where(t => t.Exception != null);
                foreach (var exTask in modifyExceptionsTaskList)
                {
                    Logger.Log(LoggerLevel.Error, LogId, logTag, $"modifyConfigTasks Exceptions thrown : {exTask.Exception.Flatten()}");
                    failedFilesCount++;
                }

                operationResult = OperationStatus.FileSaveError;
            }

            Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Detail statistics : Total {totalFilesCount} config files are correctly read and processed. {totalFilesCount - failedFilesCount} config files are modified successfully.");
            return operationResult;
        }

        /// <summary>
        /// Read a metric config file and deserialize as IMetricConfiguration.
        /// </summary>
        /// <param name="filePath">metric config file to read</param>
        /// <returns>object of IMetricConfiguration</returns>
        private IMetricConfiguration ReadFileAsMetricConfiguration(string filePath)
        {
            const string logTag = "ReadFileAsMetricConfiguration";
            string content = File.ReadAllText(filePath);
            IMetricConfiguration metricConfigurationFromFile;

            try
            {
                metricConfigurationFromFile =
                    JsonConvert.DeserializeObject<RawMetricConfiguration>(content, this.serializerSettings);
                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Processing file {filePath}, this is raw metric.");
            }
            catch
            {
                try
                {
                    metricConfigurationFromFile =
                        JsonConvert.DeserializeObject<CompositeMetricConfiguration>(content, this.serializerSettings);
                    Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Processing file {filePath}, this is composite metric.");
                }
                catch (JsonSerializationException jex)
                {
                    Logger.Log(LoggerLevel.Error, LogId, logTag, $"Cannot deserialize Json file {filePath}. Exceptions : {jex}");
                    throw;
                }
            }

            return metricConfigurationFromFile;
        }

        /// <summary>
        /// Process sucessfully retrived metric configuration objects.
        /// </summary>
        /// <param name="downloadedMetric">metric configuration</param>
        /// <param name="accountName">account name</param>
        /// <param name="downloadDefaultMetricConfig">whether save default retrieved metric configuration</param>
        /// <param name="maxFileNameProducedLength">max size of file name length</param>
        /// <param name="curFolder">folder to store this metric configuration</param>
        /// <returns>OperationStatus</returns>
        private OperationStatus ProcessRetrievedMetrics(
            IMetricConfiguration downloadedMetric,
            string accountName,
            bool downloadDefaultMetricConfig,
            int maxFileNameProducedLength,
            string curFolder)
        {
            const string logTag = "ProcessRetrievedMetrics";
            Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Processing retrieved metric {downloadedMetric.Name} configuration.");

            if (!downloadDefaultMetricConfig)
            {
                if (downloadedMetric.LastUpdatedTime == default(DateTime))
                {
                    Logger.Log(
                        LoggerLevel.CustomerFacingInfo,
                        LogId,
                        logTag,
                        $"Skipping default metric config for metric {downloadedMetric.Name} in namespace {downloadedMetric.MetricNamespace}");
                    return OperationStatus.ResourceSkipped;
                }
            }

            var fileName = FileNamePathHelper.ConstructValidFileName(
                accountName,
                downloadedMetric.MetricNamespace,
                downloadedMetric.Name,
                string.Empty,
                FileNamePathHelper.JsonFileExtension,
                maxFileNameProducedLength);

            Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Saving metric config file {fileName} ...");
            try
            {
                FileOperationHelper.SaveContentToFile(
                    Path.Combine(curFolder, fileName),
                    JsonConvert.SerializeObject(downloadedMetric, Formatting.Indented, this.serializerSettings));

                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Saved metric config file {fileName}.");
                return OperationStatus.CompleteSuccess;
            }
            catch (Exception ex)
            {
                Logger.Log(LoggerLevel.Error, LogId, logTag, $"Failed writing file {fileName}. Exception: {ex}");
                return OperationStatus.FileSaveError;
            }
        }

        /// <summary>
        /// Async modify IMetricConfiguration and save to file
        /// </summary>
        /// <param name="configFile">configuration file to save modified config</param>
        /// <param name="metricConfigFromFile">original IMetricConfiguration read from file</param>
        /// <param name="monitoringAccount">MonitoringAccount</param>
        /// <param name="replaceAccountNameWith">account name to replace original account with in local config files</param>
        /// <param name="replaceNamespaceWith">namespace to replace original namespace with in local config files</param>
        /// <returns>return a Task</returns>
        private async Task ModifyMetricConfigurationAsync(
            string configFile,
            IMetricConfiguration metricConfigFromFile,
            IMonitoringAccount monitoringAccount,
            string replaceAccountNameWith,
            string replaceNamespaceWith)
        {
            const string logTag = "ModifyMetricConfigurationAsync";
            IMetricConfiguration newMetricConfig;

            if (metricConfigFromFile is RawMetricConfiguration)
            {
                newMetricConfig = this.CopyAndReplaceRawMetricConfig(
                    (RawMetricConfiguration)metricConfigFromFile,
                    replaceAccountNameWith,
                    replaceNamespaceWith);
            }
            else
            {
                newMetricConfig = this.CopyAndReplaceCompositeMetricConfig(
                    (CompositeMetricConfiguration)metricConfigFromFile,
                    monitoringAccount,
                    replaceAccountNameWith,
                    replaceNamespaceWith);
            }

            try
            {
                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, logTag, $"Writing file {configFile}...");

                await FileOperationHelper.SaveContentToFileAsync(
                    configFile,
                    JsonConvert.SerializeObject(newMetricConfig, Formatting.Indented, this.serializerSettings)).ConfigureAwait(false);
            }
            catch
            {
                Logger.Log(LoggerLevel.Error, LogId, logTag, $"Exceptions in modifying local file {configFile}.");
                throw;
            }
        }

        /// <summary>
        /// Copy original RawMetricConfiguration and do modification.
        /// </summary>
        /// <param name="rawMetricConfigFromFile">original RawMetricConfiguration from file</param>
        /// <param name="replaceAccountNameWith">account name to replace original account with in local config files</param>
        /// <param name="replaceNamespaceWith">namespace to replace original namespace with in local config files</param>
        /// <returns>modified RawMetricConfiguration</returns>
        private RawMetricConfiguration CopyAndReplaceRawMetricConfig(
            RawMetricConfiguration rawMetricConfigFromFile,
            string replaceAccountNameWith,
            string replaceNamespaceWith)
        {
            if (!string.IsNullOrWhiteSpace(replaceAccountNameWith))
            {
                Logger.Log(LoggerLevel.CustomerFacingInfo, LogId, "CopyAndReplaceRawMetricConfig", $"Metric {rawMetricConfigFromFile.Name} is RawMetricConfiguration and ReplaceAccountNameWith is not applicable.");
            }

            var newMetricNamespace = string.IsNullOrWhiteSpace(replaceNamespaceWith)
                ? rawMetricConfigFromFile.MetricNamespace
                : replaceNamespaceWith;

            return new RawMetricConfiguration(
                newMetricNamespace,
                rawMetricConfigFromFile.Name,
                rawMetricConfigFromFile.LastUpdatedTime,
                rawMetricConfigFromFile.LastUpdatedBy,
                rawMetricConfigFromFile.Version,
                rawMetricConfigFromFile.ScalingFactor,
                rawMetricConfigFromFile.EnableClientPublication,
                rawMetricConfigFromFile.EnableClientForking,
                rawMetricConfigFromFile.Description,
                rawMetricConfigFromFile.Dimensions,
                rawMetricConfigFromFile.Preaggregations,
                rawMetricConfigFromFile.RawSamplingTypes,
                rawMetricConfigFromFile.ComputedSamplingTypes,
                rawMetricConfigFromFile.EnableClientSideLastSamplingMode,
                rawMetricConfigFromFile.EnableClientEtwPublication);
        }

        /// <summary>
        /// Copy original CompositeMetricConfiguration and do modification.
        /// </summary>
        /// <param name="compositeMetricConfigFromFile">original CompositeMetricConfiguration from file</param>
        /// <param name="monitoringAccount">MonitoringAccount</param>
        /// <param name="replaceAccountNameWith">account name to replace original account with in local config files</param>
        /// <param name="replaceNamespaceWith">namespace to replace original namespace with in local config files</param>
        /// <returns>modified CompositeMetricConfiguration</returns>
        private CompositeMetricConfiguration CopyAndReplaceCompositeMetricConfig(
            CompositeMetricConfiguration compositeMetricConfigFromFile,
            IMonitoringAccount monitoringAccount,
            string replaceAccountNameWith,
            string replaceNamespaceWith)
        {
            var newAccountName = string.IsNullOrWhiteSpace(replaceAccountNameWith)
                ? monitoringAccount.Name
                : replaceAccountNameWith;

            var newMetricNamespace = string.IsNullOrWhiteSpace(replaceNamespaceWith)
                ? compositeMetricConfigFromFile.MetricNamespace
                : replaceNamespaceWith;

            // deep copy metric sources by replacing matching account name and namespace at same time
            var newMetricSources = new List<CompositeMetricSource>();
            foreach (var source in compositeMetricConfigFromFile.MetricSources)
            {
                var newSourceAccountName =
                    source.MonitoringAccount.Equals(monitoringAccount.Name, StringComparison.OrdinalIgnoreCase)
                        ? newAccountName
                        : source.MonitoringAccount;
                var newSourceNamespace =
                    source.MetricNamespace.Equals(compositeMetricConfigFromFile.MetricNamespace, StringComparison.OrdinalIgnoreCase)
                        ? newMetricNamespace
                        : source.MetricNamespace;

                newMetricSources.Add(new CompositeMetricSource(source.DisplayName, newSourceAccountName, newSourceNamespace, source.Metric));
            }

            var newCompositeMetricConfig = new CompositeMetricConfiguration(
                newMetricNamespace,
                compositeMetricConfigFromFile.Name,
                compositeMetricConfigFromFile.LastUpdatedTime,
                compositeMetricConfigFromFile.LastUpdatedBy,
                compositeMetricConfigFromFile.Version,
                compositeMetricConfigFromFile.TreatMissingSeriesAsZeroes,
                compositeMetricConfigFromFile.Description,
                newMetricSources,
                compositeMetricConfigFromFile.CompositeExpressions);

            return newCompositeMetricConfig;
        }

        /// <summary>
        /// Apply template IMetricConfiguration object by replacing MetricNamespace and Name.
        /// </summary>
        /// <param name="metricConfigTemplate">template IMetricConfiguration read from file</param>
        /// <param name="targetNamespace">target MetricNamespace</param>
        /// <param name="targetMetricName">target metric Name</param>
        /// <returns>IMetricConfiguration</returns>
        private IMetricConfiguration ApplyTemplateConfigWithDifferentMetric(
            IMetricConfiguration metricConfigTemplate,
            string targetNamespace,
            string targetMetricName)
        {
            IMetricConfiguration newMetricConfig;

            if (metricConfigTemplate is RawMetricConfiguration)
            {
                var rawMetricConfigTemplate = (RawMetricConfiguration)metricConfigTemplate;
                newMetricConfig = new RawMetricConfiguration(
                    targetNamespace,
                    targetMetricName,
                    rawMetricConfigTemplate.LastUpdatedTime,
                    rawMetricConfigTemplate.LastUpdatedBy,
                    rawMetricConfigTemplate.Version,
                    rawMetricConfigTemplate.ScalingFactor,
                    rawMetricConfigTemplate.EnableClientPublication,
                    rawMetricConfigTemplate.EnableClientForking,
                    rawMetricConfigTemplate.Description,
                    rawMetricConfigTemplate.Dimensions,
                    rawMetricConfigTemplate.Preaggregations,
                    rawMetricConfigTemplate.RawSamplingTypes,
                    rawMetricConfigTemplate.ComputedSamplingTypes,
                    rawMetricConfigTemplate.EnableClientSideLastSamplingMode,
                    rawMetricConfigTemplate.EnableClientEtwPublication);
            }
            else
            {
                var compositeMetricConfigTemplate = (CompositeMetricConfiguration)metricConfigTemplate;
                newMetricConfig = new CompositeMetricConfiguration(
                    targetNamespace,
                    targetMetricName,
                    compositeMetricConfigTemplate.LastUpdatedTime,
                    compositeMetricConfigTemplate.LastUpdatedBy,
                    compositeMetricConfigTemplate.Version,
                    compositeMetricConfigTemplate.TreatMissingSeriesAsZeroes,
                    compositeMetricConfigTemplate.Description,
                    compositeMetricConfigTemplate.MetricSources,
                    compositeMetricConfigTemplate.CompositeExpressions);
            }

            return newMetricConfig;
        }

        /// <summary>
        /// true if this is a default configuration.
        /// </summary>
        /// <param name="metricConfiguration">Monitoring Account</param>
        /// <returns>true if default configuration returned.</returns>
        private bool IsDefaultMetric(IMetricConfiguration metricConfiguration)
        {
            const string logTag = "IsDefaultMetricAsync";
            if (metricConfiguration == null)
            {
                Logger.Log(LoggerLevel.Error, LogId, logTag, $"Argument MetricConfiguration [{metricConfiguration.MetricNamespace}][{metricConfiguration.Name}] is NULL.");
                throw new ArgumentNullException(nameof(metricConfiguration));
            }

            return metricConfiguration.LastUpdatedTime == default(DateTime);
        }

        /// <summary>
        /// A helper method which waits for all given sync all tasks to complete.
        /// </summary>
        /// <param name="taskList">The task list.</param>
        /// <param name="results">The results.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        private async Task WaitAllForSyncAllAsyncV2(
            List<Task<ConfigurationUpdateResultList>> taskList,
            List<ConfigurationUpdateResultList> results)
        {
            const string logTag = "SyncAllAsyncV2";
            try
            {
                await Task.WhenAll(taskList).ConfigureAwait(false);
                foreach (var task in taskList)
                {
                    if (task.Result.Success)
                    {
                        // For metric configuration in QOS or other internal namespace, there is no configuration
                        // to replicate and thus the updates results is an empty list.
                        if (task.Result.ConfigurationUpdateResults.Count > 0)
                        {
                            results.Add(task.Result);
                        }
                    }
                    else
                    {
                        if (task.Result.ExceptionMessage.Contains(
                            "Event configuration to be updated can't be null."))
                        {
                            // No configuration exist for the specified metrics and hence nothing
                            // to replicate.
                            continue;
                        }

                        results.Add(task.Result);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(
                    LoggerLevel.Error,
                    LogId,
                    logTag,
                    $"Exception occured while replicating configuration. Exception: {ex}");
            }
        }
    }
}
