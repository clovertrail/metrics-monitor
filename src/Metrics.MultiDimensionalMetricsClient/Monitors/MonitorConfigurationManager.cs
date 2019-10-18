// -----------------------------------------------------------------------
// <copyright file="MonitorConfigurationManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Configuration;
    using Logging;
    using Metrics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Utility;

    /// <summary>
    /// Monitor configuration manager.
    /// </summary>
    public sealed class MonitorConfigurationManager : IMonitorConfigurationManager
    {
        private static readonly object LogId = Logger.CreateCustomLogId(nameof(MonitorConfigurationManager));
        private static readonly string LogTag = nameof(MonitorConfigurationManager);
        private readonly ConnectionInfo connectionInfo;
        private readonly HttpClient httpClient;
        private readonly string metricUrlPrefix;
        private readonly IMetricReader metricReader;

        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false
                }
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorConfigurationManager" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information for the endpoint being used.</param>
        public MonitorConfigurationManager(ConnectionInfo connectionInfo)
            : this(connectionInfo, HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo), new MetricReader(connectionInfo))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorConfigurationManager" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information for the endpoint being used.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="metricReader">The metric reader.</param>
        internal MonitorConfigurationManager(ConnectionInfo connectionInfo, HttpClient httpClient, IMetricReader metricReader)
        {
            this.connectionInfo = connectionInfo;
            this.metricUrlPrefix = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.ConfigRelativeUrl);
            this.httpClient = httpClient;
            this.metricReader = metricReader;
        }

        /// <summary>
        /// Gets or sets the maximum parallel running tasks.
        /// </summary
        public int MaxParallelRunningTasks { get; set; } = 20;

        /// <inheritdoc />
        public async Task<ConfigurationUpdateResultList> SyncConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            string metricName,
            bool skipVersionCheck = false,
            bool validate = true)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (metricNamespace == null)
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            if (metricName == null)
            {
                throw new ArgumentNullException(nameof(metricName));
            }

            var operation = $"{this.metricUrlPrefix}/replicateMonitorConfigurations";
            var path =
                $"{operation}/monitoringAccount/{monitoringAccount.Name}/metricNamespace/{metricNamespace}/metricName/{metricName}/skipVersionCheck/{skipVersionCheck}/operation/Replace";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetGlobalEndpoint())
            {
                Path = path,
                Query = $"validate={validate}"
            };

            var result = new ConfigurationUpdateResultList
            {
                MonitoringAccount = monitoringAccount.Name,
                MetricNamespace = metricNamespace,
                MetricName = metricName
            };

            try
            {
                if (monitoringAccount.MirrorMonitoringAccountList == null || !monitoringAccount.MirrorMonitoringAccountList.Any())
                {
                    throw new Exception("MirrorAccountsList can't be null or empty while replicating monitors.");
                }

                var serializedTargetAccounts = JsonConvert.SerializeObject(monitoringAccount.MirrorMonitoringAccountList.ToList(), Formatting.Indented, this.serializerSettings);
                var response = await HttpClientHelper.GetResponse(
                    uriBuilder.Uri,
                    HttpMethod.Post,
                    this.httpClient,
                    monitoringAccount.Name,
                    operation,
                    serializedContent: serializedTargetAccounts).ConfigureAwait(false);

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

                if (mce.ResponseStatusCode == HttpStatusCode.Unauthorized || mce.ResponseStatusCode == HttpStatusCode.Forbidden)
                {
                    var exMsg =
                        $"Unable to sync configuration for monitoringAccount:{monitoringAccount.Name}, metricNamespace:{metricNamespace}, metricName:{metricName}"
                        + $"doesn't have permission to update configurations in mirror accounts. Response:{mce.Message}";

                    throw new ConfigurationValidationException(exMsg, ValidationType.ServerSide, mce);
                }

                result.ExceptionMessage = mce.Message;

                return result;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            string metricNamespace,
            bool skipVersionCheck = false,
            bool validate = true)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrEmpty(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            var metricNames = await this.metricReader.GetMetricNamesAsync(
                monitoringAccount.Name,
                metricNamespace).ConfigureAwait(false);
            var taskList = new List<Task<ConfigurationUpdateResultList>>(this.MaxParallelRunningTasks);

            List<ConfigurationUpdateResultList> results = new List<ConfigurationUpdateResultList>();
            foreach (var metricName in metricNames)
            {
                if (taskList.Count == this.MaxParallelRunningTasks)
                {
                    await this.WaitForSync(taskList, results).ConfigureAwait(false);
                    taskList.Clear();
                }
                else
                {
                    taskList.Add(this.SyncConfigurationAsync(monitoringAccount, metricNamespace, metricName, skipVersionCheck, validate));
                }
            }

            if (taskList.Count > 0)
            {
                await this.WaitForSync(taskList, results).ConfigureAwait(false);
                taskList.Clear();
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ConfigurationUpdateResultList>> SyncConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            bool skipVersionCheck = false,
            bool validate = true)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            var namespaces =
                await this.metricReader.GetNamespacesAsync(monitoringAccount.Name).ConfigureAwait(false);

            List<ConfigurationUpdateResultList> results = new List<ConfigurationUpdateResultList>();
            foreach (var ns in namespaces)
            {
                var namespaceResults = await this.SyncConfigurationAsync(
                    monitoringAccount,
                    ns,
                    skipVersionCheck,
                    validate).ConfigureAwait(false);

                // For QOS namespace or other internal namespace, there is no configuration to
                // replicate and thus the namespace results is an empty list.
                if (namespaceResults.Count > 0)
                {
                    results.AddRange(namespaceResults);
                }
            }

            return results;
        }

        /// <summary>
        /// A helper method which waits for all given sync all tasks to complete.
        /// </summary>
        /// <param name="taskList">The task list.</param>
        /// <param name="results">The results.</param>
        /// <returns>An awaitable <see cref="Task" />.</returns>
        private async Task WaitForSync(
            List<Task<ConfigurationUpdateResultList>> taskList,
            List<ConfigurationUpdateResultList> results)
        {
            try
            {
                await Task.WhenAll(taskList).ConfigureAwait(false);
                foreach (var task in taskList)
                {
                    if (task.Result.Success)
                    {
                        if (task.Result.ConfigurationUpdateResults.Count > 0)
                        {
                            results.Add(task.Result);
                        }
                    }
                    else
                    {
                        if (task.Result.ExceptionMessage.Contains(
                            "Monitor configuration to be updated can't be null."))
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
                    LogTag,
                    $"Exception occurred while replicating configuration. Exception: {ex}");
            }
        }
    }
}