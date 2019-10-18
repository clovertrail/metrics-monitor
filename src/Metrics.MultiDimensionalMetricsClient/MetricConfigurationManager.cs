// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricConfigurationManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Cloud.Metrics.Client.Utility;
    using Microsoft.Online.Metrics.Serialization.Configuration;

    using Newtonsoft.Json;

    /// <summary>
    /// The class to read and write metric configurations.
    /// </summary>
    internal sealed class MetricConfigurationManager
    {
#pragma warning disable SA1401 // Fields must be private
        /// <summary>
        /// The relative URL for metrics configuration
        /// </summary>
        public readonly string ConfigRelativeUrl;
#pragma warning restore SA1401 // Fields must be private

        /// <summary>
        /// The HTTP client instance.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// The connection information
        /// </summary>
        private readonly ConnectionInfo connectionInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricConfigurationManager" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        public MetricConfigurationManager(ConnectionInfo connectionInfo)
        {
            this.connectionInfo = connectionInfo;
            this.ConfigRelativeUrl = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.ConfigRelativeUrl);
            this.httpClient = HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo);
        }

        /// <summary>
        /// Gets the metric configuration.
        /// </summary>
        /// <param name="metricIdentifier">The metric identifier.</param>
        /// <returns>The metric configuration.</returns>
        public async Task<MetricConfigurationV2> Get(MetricIdentifier metricIdentifier)
        {
            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace/{3}/metric/{4}",
                this.connectionInfo.GetEndpoint(metricIdentifier.MonitoringAccount),
                this.ConfigRelativeUrl,
                metricIdentifier.MonitoringAccount,
                SpecialCharsHelper.EscapeTwice(metricIdentifier.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(metricIdentifier.MetricName));

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Get,
                this.httpClient,
                metricIdentifier.MonitoringAccount,
                this.ConfigRelativeUrl).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<MetricConfigurationV2>(response.Item1);
        }

        /// <summary>
        /// Deletes the metric configuration.
        /// </summary>
        /// <param name="metricIdentifier">The metric identifier.</param>
        /// <returns>A task representing the deletion of the configuration.</returns>
        /// <remarks>
        /// It deletes only the metric configuration, not the metric data.
        /// </remarks>
        public async Task Delete(MetricIdentifier metricIdentifier)
        {
            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace/{3}/metric/{4}",
                this.connectionInfo.GetEndpoint(metricIdentifier.MonitoringAccount),
                this.ConfigRelativeUrl,
                metricIdentifier.MonitoringAccount,
                SpecialCharsHelper.EscapeTwice(metricIdentifier.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(metricIdentifier.MetricName));

            await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Delete,
                this.httpClient,
                metricIdentifier.MonitoringAccount,
                this.ConfigRelativeUrl).ConfigureAwait(false);
        }

        /// <summary>
        /// Posts the metric configuration.
        /// </summary>
        /// <param name="configuration">The metric configuration.</param>
        /// <returns>A task representing the update of the configuration.</returns>
        public async Task Post(MetricConfigurationV2 configuration)
        {
            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace/{3}/metric/{4}",
                this.connectionInfo.GetEndpoint(configuration.MonitoringAccount),
                this.ConfigRelativeUrl,
                configuration.MonitoringAccount,
                SpecialCharsHelper.EscapeTwice(configuration.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(configuration.MetricName));

            await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Post,
                this.httpClient,
                configuration.MonitoringAccount,
                this.ConfigRelativeUrl,
                configuration).ConfigureAwait(false);
        }
    }
}