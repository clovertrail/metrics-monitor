// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PreaggregateFiltersManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.PreaggregateFiltersManagement
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using MetricEnrichmentRuleManagement;
    using Newtonsoft.Json;
    using Utility;

    /// <summary>
    /// This class manages get and save operations on preaggregate filters.
    /// </summary>
    public sealed class PreaggregateFiltersManager
    {
        private readonly ConnectionInfo connectionInfo;
        private readonly HttpClient httpClient;
        private readonly string configurationUrlPrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreaggregateFiltersManager"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information for the MDM endpoint being used.</param>
        public PreaggregateFiltersManager(ConnectionInfo connectionInfo)
        {
            if (connectionInfo == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            this.connectionInfo = connectionInfo;
            this.configurationUrlPrefix = this.connectionInfo.GetAuthRelativeUrl("v1/config/preaggregate/dimensionfilters/");

            this.httpClient = HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo);
        }

        /// <summary>
        /// Gets all filters satisfying the given constraints.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="preaggregateDimensionNames">The preaggregate dimension names.</param>
        /// <param name="count">The count of filters requested. Use 0 to get all filters.</param>
        /// <param name="pageOffset">The offset of the requested filters page calculated based on the count of data returned.</param>
        /// <returns>All enrichment rules for the given monitoring account.</returns>
        public async Task<PreaggregateFilters> GetPreaggregateFiltersAsync(
            string monitoringAccount,
            string metricNamespace,
            string metricName,
            IEnumerable<string> preaggregateDimensionNames,
            int count,
            int pageOffset)
        {
            if (string.IsNullOrWhiteSpace(monitoringAccount))
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            var arguments = new RawPreaggregateFilterQueryArguments(monitoringAccount, metricNamespace, metricName, preaggregateDimensionNames, count, pageOffset);

            string serializedArguments = JsonConvert.SerializeObject(arguments);
            string path = $"{this.configurationUrlPrefix}monitoringAccount/{monitoringAccount}/getfilters";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount))
            {
                Path = path
            };

            var response = await HttpClientHelper.GetResponseAsStringAsync(
                uriBuilder.Uri,
                HttpMethod.Post,
                this.httpClient,
                monitoringAccount,
                this.configurationUrlPrefix,
                serializedContent: serializedArguments).ConfigureAwait(false);

            var rules = JsonConvert.DeserializeObject<PreaggregateFilters>(response);
            return rules;
        }

        /// <summary>
        /// Adds the given pre-aggregate filters to storage for given monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account for the enrichment rule.</param>
        /// <param name="preaggregateFilters">Preaggregate filters which needs to be added to the storage.</param>
        /// <returns>Task representing the operation.</returns>
        public async Task AddPreaggregateFilters(string monitoringAccount, PreaggregateFilters preaggregateFilters)
        {
            if (string.IsNullOrEmpty(monitoringAccount))
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (preaggregateFilters == null)
            {
                throw new ArgumentNullException(nameof(preaggregateFilters));
            }

            string path = $"{this.configurationUrlPrefix}monitoringAccount/{monitoringAccount}/addfilters";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount))
            {
                Path = path,
                Query = "apiVersion=1"
            };

            string serializedContent = JsonConvert.SerializeObject(preaggregateFilters);

            await HttpClientHelper.GetResponseAsStringAsync(
                uriBuilder.Uri,
                HttpMethod.Post,
                this.httpClient,
                monitoringAccount,
                this.configurationUrlPrefix,
                serializedContent: serializedContent).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes the given pre-aggregate filters from storage for given monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account for the enrichment rule.</param>
        /// <param name="preaggregateFilters">Preaggregate filters which needs to be removed from the storage.</param>
        /// <returns>Task representing the operation.</returns>
        public async Task RemovePreaggregateFilters(string monitoringAccount, PreaggregateFilters preaggregateFilters)
        {
            if (string.IsNullOrEmpty(monitoringAccount))
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (preaggregateFilters == null)
            {
                throw new ArgumentNullException(nameof(preaggregateFilters));
            }

            string path = $"{this.configurationUrlPrefix}monitoringAccount/{monitoringAccount}/removefilters";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount))
            {
                Path = path,
                Query = "apiVersion=1"
            };

            string serializedContent = JsonConvert.SerializeObject(preaggregateFilters);

            await HttpClientHelper.GetResponseAsStringAsync(
                uriBuilder.Uri,
                HttpMethod.Delete,
                this.httpClient,
                monitoringAccount,
                this.configurationUrlPrefix,
                serializedContent: serializedContent).ConfigureAwait(false);
        }
    }
}
