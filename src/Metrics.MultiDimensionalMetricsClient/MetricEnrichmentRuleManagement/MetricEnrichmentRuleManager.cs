// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricEnrichmentRuleManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.MetricEnrichmentRuleManagement
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Cloud.Metrics.Client.Utility;

    using Newtonsoft.Json;

    /// <summary>
    /// This class manages get and save operations on metric enrichment rules.
    /// </summary>
    public sealed class MetricEnrichmentRuleManager
    {
        private readonly ConnectionInfo connectionInfo;
        private readonly HttpClient httpClient;
        private readonly string configurationUrlPrefix;
        private readonly JsonSerializerSettings serializerSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricEnrichmentRuleManager"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information for the MDM endpoint being used.</param>
        public MetricEnrichmentRuleManager(ConnectionInfo connectionInfo)
        {
            if (connectionInfo == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            this.connectionInfo = connectionInfo;
            this.configurationUrlPrefix = this.connectionInfo.GetAuthRelativeUrl("v1/config/enrichmentrules/");

            this.httpClient = HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo);

            this.serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
        }

        /// <summary>
        /// Gets all the enrichment rules configured for given monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <returns>All enrichment rules for the given monitoring account.</returns>
        public async Task<IReadOnlyList<MetricEnrichmentRule>> GetAllAsync(string monitoringAccount)
        {
            if (string.IsNullOrWhiteSpace(monitoringAccount))
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            var path = $"{this.configurationUrlPrefix}getAll/monitoringAccount/{monitoringAccount}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount))
            {
                Path = path
            };

            var response = await HttpClientHelper.GetResponseAsStringAsync(
                uriBuilder.Uri,
                HttpMethod.Get,
                this.httpClient,
                monitoringAccount,
                this.configurationUrlPrefix).ConfigureAwait(false);

            var rules = JsonConvert.DeserializeObject<List<MetricEnrichmentRule>>(response, this.serializerSettings);
            return rules;
        }

        /// <summary>
        /// Save the metric configuration provided.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration.</param>
        /// <param name="rule">Rule to save.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task SaveAsync(string monitoringAccount, MetricEnrichmentRule rule)
        {
            if (string.IsNullOrEmpty(monitoringAccount))
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var validationFailureMessage = rule.Validate();
            if (!string.IsNullOrEmpty(validationFailureMessage))
            {
                throw new ArgumentException(validationFailureMessage);
            }

            var path = $"{this.configurationUrlPrefix}monitoringAccount/{monitoringAccount}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount))
            {
                Path = path,
                Query = "apiVersion=1"
            };

            var serializedMetric = JsonConvert.SerializeObject(rule);

            await HttpClientHelper.GetResponseAsStringAsync(
                uriBuilder.Uri,
                HttpMethod.Post,
                this.httpClient,
                monitoringAccount,
                this.configurationUrlPrefix,
                serializedContent: serializedMetric).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the enrichment rule.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="rule">Rule to be deleted.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task DeleteAsync(string monitoringAccount, MetricEnrichmentRule rule)
        {
            if (string.IsNullOrEmpty(monitoringAccount))
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var validationFailureMessage = rule.Validate();
            if (!string.IsNullOrEmpty(validationFailureMessage))
            {
                throw new ArgumentException(validationFailureMessage);
            }

            var path = $"{this.configurationUrlPrefix}monitoringAccount/{monitoringAccount}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount))
            {
                Path = path
            };

            var serializedMetric = JsonConvert.SerializeObject(rule, Formatting.Indented, this.serializerSettings);
            await HttpClientHelper.GetResponseAsStringAsync(
                uriBuilder.Uri,
                HttpMethod.Delete,
                this.httpClient,
                monitoringAccount,
                this.configurationUrlPrefix,
                serializedContent: serializedMetric).ConfigureAwait(false);
        }
    }
}
