// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StampLevelMetricEnrichmentRuleManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.MetricEnrichmentRuleManagement
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using MetricEnrichmentRuleManagement;
    using Microsoft.Cloud.Metrics.Client.Utility;

    using Newtonsoft.Json;

    /// <summary>
    /// This class manages get and save operations on stamp level metric enrichment rules ( only service admins are authorized to modify stamp level rules).
    /// </summary>
    public sealed class StampLevelMetricEnrichmentRuleManager
    {
        private readonly ConnectionInfo connectionInfo;
        private readonly HttpClient httpClient;
        private readonly string configurationUrlPrefix;
        private readonly JsonSerializerSettings serializerSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="StampLevelMetricEnrichmentRuleManager"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information for the MDM endpoint being used.</param>
        public StampLevelMetricEnrichmentRuleManager(ConnectionInfo connectionInfo)
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
        /// <returns>All enrichment rules for the given monitoring account.</returns>
        public async Task<IReadOnlyList<MetricEnrichmentRule>> GetAllAsync()
        {
            var path = $"{this.configurationUrlPrefix}getAll";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(string.Empty))
            {
                Path = path
            };

            var response = await HttpClientHelper.GetResponseAsStringAsync(
                uriBuilder.Uri,
                HttpMethod.Get,
                this.httpClient,
                string.Empty,
                this.configurationUrlPrefix).ConfigureAwait(false);

            var rules = JsonConvert.DeserializeObject<List<MetricEnrichmentRule>>(response, this.serializerSettings);
            return rules;
        }

        /// <summary>
        /// Save the metric configuration provided.
        /// </summary>
        /// <param name="rule">Rule to save.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task SaveAsync(MetricEnrichmentRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var validationFailureMessage = rule.Validate();
            if (!string.IsNullOrEmpty(validationFailureMessage))
            {
                throw new ArgumentException(validationFailureMessage);
            }

            if (!rule.MonitoringAccountFilter.Equals("*"))
            {
                throw new ArgumentException("Monitoring account needs to be * as this is stamp level rule.");
            }

            var path = $"{this.configurationUrlPrefix}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(string.Empty))
            {
                Path = path,
                Query = "apiVersion=1"
            };

            var serializedMetric = JsonConvert.SerializeObject(rule, Formatting.Indented, this.serializerSettings);

            await HttpClientHelper.GetResponseAsStringAsync(
                uriBuilder.Uri,
                HttpMethod.Post,
                this.httpClient,
                string.Empty,
                this.configurationUrlPrefix,
                serializedContent: serializedMetric).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the enrichment rule.
        /// </summary>
        /// <param name="rule">Rule to be deleted.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task DeleteAsync(MetricEnrichmentRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var validationFailureMessage = rule.Validate();
            if (!string.IsNullOrEmpty(validationFailureMessage))
            {
                throw new ArgumentException(validationFailureMessage);
            }

            var path = $"{this.configurationUrlPrefix}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(string.Empty))
            {
                Path = path
            };

            var serializedMetric = JsonConvert.SerializeObject(rule, Formatting.Indented, this.serializerSettings);
            await HttpClientHelper.GetResponseAsStringAsync(
                uriBuilder.Uri,
                HttpMethod.Delete,
                this.httpClient,
                string.Empty,
                this.configurationUrlPrefix,
                serializedContent: serializedMetric).ConfigureAwait(false);
        }
    }
}
