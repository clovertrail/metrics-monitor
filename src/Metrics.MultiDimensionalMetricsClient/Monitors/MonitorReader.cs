// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MonitorReader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Cloud.Metrics.Client.Metrics;
    using Microsoft.Cloud.Metrics.Client.Utility;
    using Microsoft.Online.Metrics.Serialization;
    using Microsoft.Online.Metrics.Serialization.Configuration;
    using Microsoft.Online.Metrics.Serialization.Monitor;

    using Newtonsoft.Json;

    /// <summary>
    /// The class to query monitor health status.
    /// </summary>
    public sealed class MonitorReader : IMonitorReader
    {
        /// <summary>
        /// The number of attempts.
        /// </summary>
        public const int NumAttempts = 3;

#pragma warning disable SA1401 // Fields must be private
        /// <summary>
        /// The relative URL for health controller
        /// </summary>
        public readonly string HealthRelativeUrl;

        /// <summary>
        /// The V2 relative URL for metrics configuration
        /// </summary>
        public readonly string ConfigRelativeUrlV2;
#pragma warning restore SA1401 // Fields must be private

        /// <summary>
        /// The status sampling type.
        /// </summary>
        private static readonly SamplingType StatusSamplingType = new SamplingType("Status");

        /// <summary>
        /// The HTTP client instance.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// The connection information.
        /// </summary>
        private readonly ConnectionInfo connectionInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorReader"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        public MonitorReader(ConnectionInfo connectionInfo)
        {
            if (connectionInfo == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            this.connectionInfo = connectionInfo;
            this.HealthRelativeUrl = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.HealthRelativeUrl);
            this.ConfigRelativeUrlV2 = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.ConfigRelativeUrlV2);
            this.httpClient = HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo);

            this.GetResponseAsStringDelegate = HttpClientHelper.GetResponse;
        }

        /// <summary>
        /// Gets or sets the delegate to get web response as string.
        /// </summary>
        /// <remarks>For unit testing.</remarks>
        internal Func<Uri, HttpMethod, HttpClient, string, string, object, string, string, Guid?, byte, int, Task<Tuple<string, HttpResponseMessage>>> GetResponseAsStringDelegate { get; set; }

        /// <summary>
        /// Gets the monitors for the given <paramref name="metricIdentifier"/>.
        /// </summary>
        /// <param name="metricIdentifier">The metric identifier.</param>
        /// <returns>The monitors for the given <paramref name="metricIdentifier"/>.</returns>
        public async Task<IReadOnlyList<MonitorIdentifier>> GetMonitorsAsync(MetricIdentifier metricIdentifier)
        {
            metricIdentifier.Validate();

            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace/{3}/metric/{4}/monitorIDs",
                this.connectionInfo.GetEndpoint(metricIdentifier.MonitoringAccount),
                this.ConfigRelativeUrlV2,
                metricIdentifier.MonitoringAccount,
                SpecialCharsHelper.EscapeTwice(metricIdentifier.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(metricIdentifier.MetricName));

            var response = await this.GetResponseAsStringDelegate(
                new Uri(url),
                HttpMethod.Get,
                this.httpClient,
                metricIdentifier.MonitoringAccount,
                this.ConfigRelativeUrlV2,
                null,
                string.Empty,
                null,
                null,
                MetricQueryResponseDeserializer.CurrentVersion,
                NumAttempts).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<MonitorIdentifier[]>(response.Item1);
        }

        /// <summary>
        /// Gets the monitor IDs for the given monitoring account, optionally with the metric namespace.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>
        /// The monitor IDs for the given monitoring account, optionally with the metric namespace.
        /// </returns>
        public async Task<IReadOnlyList<MonitorIdentifier>> GetMonitorsAsync(string monitoringAccount, string metricNamespace = null)
        {
            if (string.IsNullOrWhiteSpace(monitoringAccount))
            {
                throw new ArgumentException("monitoringAccount is null or empty.", monitoringAccount);
            }

            string namespaceSegments = string.Empty;
            if (!string.IsNullOrWhiteSpace(metricNamespace))
            {
                namespaceSegments = string.Format("metricNamespace/{0}", SpecialCharsHelper.EscapeTwice(metricNamespace));
            }

            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/{3}/monitorIDs",
                this.connectionInfo.GetEndpoint(monitoringAccount),
                this.ConfigRelativeUrlV2,
                monitoringAccount,
                namespaceSegments);

            var response = await this.GetResponseAsStringDelegate(
                new Uri(url),
                HttpMethod.Get,
                this.httpClient,
                monitoringAccount,
                this.ConfigRelativeUrlV2,
                null,
                string.Empty,
                null,
                null,
                MetricQueryResponseDeserializer.CurrentVersion,
                NumAttempts).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<MonitorIdentifier[]>(response.Item1);
        }

        /// <summary>
        /// Gets the current heath status asynchronous.
        /// Deprecated due to wrong spelling ("Heath" instead of "Health") Exists for backward compatibility
        /// </summary>
        /// <param name="monitorInstanceDefinition">The monitor instance definition.</param>
        /// <returns>
        /// Monitor health status.
        /// </returns>
        public async Task<IMonitorHealthStatus> GetCurrentHeathStatusAsync(TimeSeriesDefinition<MonitorIdentifier> monitorInstanceDefinition)
        {
            return await this.GetCurrentHealthStatusAsync(monitorInstanceDefinition).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the current monitor health status.
        /// </summary>
        /// <param name="monitorInstanceDefinition">The monitor instance definition.</param>
        /// <returns>
        /// The current monitor health status.
        /// </returns>
        public async Task<IMonitorHealthStatus> GetCurrentHealthStatusAsync(TimeSeriesDefinition<MonitorIdentifier> monitorInstanceDefinition)
        {
            if (monitorInstanceDefinition == null)
            {
                throw new ArgumentNullException(nameof(monitorInstanceDefinition));
            }

            var statuses = await this.GetMultipleCurrentHeathStatusesAsync(monitorInstanceDefinition).ConfigureAwait(false);
            return statuses.First().Value;
        }

        /// <summary>
        /// Batched API to get the current monitor health statuses.
        /// </summary>
        /// <param name="monitorInstanceDefinitions">The monitor instance definitions.</param>
        /// <returns>
        /// The current monitor health statuses.
        /// </returns>
        public Task<IReadOnlyList<KeyValuePair<TimeSeriesDefinition<MonitorIdentifier>, IMonitorHealthStatus>>> GetMultipleCurrentHeathStatusesAsync(
            params TimeSeriesDefinition<MonitorIdentifier>[] monitorInstanceDefinitions)
        {
            return this.GetMultipleCurrentHeathStatusesAsync(monitorInstanceDefinitions.AsEnumerable());
        }

        /// <summary>
        /// Batched API to get the current monitor health statuses.
        /// </summary>
        /// <param name="monitorInstanceDefinitions">The monitor instance definitions.</param>
        /// <returns>
        /// The current monitor health statuses.
        /// </returns>
        public async Task<IReadOnlyList<KeyValuePair<TimeSeriesDefinition<MonitorIdentifier>, IMonitorHealthStatus>>> GetMultipleCurrentHeathStatusesAsync(
            IEnumerable<TimeSeriesDefinition<MonitorIdentifier>> monitorInstanceDefinitions)
        {
            if (monitorInstanceDefinitions == null)
            {
                throw new ArgumentNullException("monitorInstanceDefinitions");
            }

            var definitionList = monitorInstanceDefinitions.ToList();

            if (definitionList.Count == 0)
            {
                throw new ArgumentException("The count of 'monitorInstanceDefinitions' is 0.");
            }

            if (definitionList.Any(d => d == null))
            {
                throw new ArgumentException("At least one of monitorInstanceDefinitions are null.");
            }

            var monitorIdentifier = definitionList[0].Id;
            var dimensionCombinationList = new List<Dictionary<string, string>>(definitionList.Count);

            foreach (var definition in definitionList)
            {
                if (!definition.Id.Equals(monitorIdentifier))
                {
                    throw new MetricsClientException("All the time series definitions must have the same monitor identifier.");
                }

                var dict = new Dictionary<string, string>();
                if (definition.DimensionCombination != null)
                {
                    foreach (var kvp in definition.DimensionCombination)
                    {
                        dict[kvp.Key] = kvp.Value;
                    }
                }

                dimensionCombinationList.Add(dict);
            }

            string operation = $"{this.HealthRelativeUrl}/batchedRead";

            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace/{3}/metric/{4}/monitorId/{5}",
                this.connectionInfo.GetEndpoint(monitorIdentifier.MetricIdentifier.MonitoringAccount),
                operation,
                monitorIdentifier.MetricIdentifier.MonitoringAccount,
                SpecialCharsHelper.EscapeTwice(monitorIdentifier.MetricIdentifier.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(monitorIdentifier.MetricIdentifier.MetricName),
                SpecialCharsHelper.EscapeTwice(monitorIdentifier.MonitorId));

            var response = await this.GetResponseAsStringDelegate(
                new Uri(url),
                HttpMethod.Post,
                this.httpClient,
                monitorIdentifier.MetricIdentifier.MonitoringAccount,
                operation,
                dimensionCombinationList,
                string.Empty,
                null,
                null,
                MetricQueryResponseDeserializer.CurrentVersion,
                NumAttempts).ConfigureAwait(false);

            var deserializeObject = JsonConvert.DeserializeObject<List<MonitorHealthStatus>>(response.Item1);
            var results = new KeyValuePair<TimeSeriesDefinition<MonitorIdentifier>, IMonitorHealthStatus>[deserializeObject.Count];

            for (int i = 0; i < deserializeObject.Count; ++i)
            {
                var status = new KeyValuePair<TimeSeriesDefinition<MonitorIdentifier>, IMonitorHealthStatus>(definitionList[i], deserializeObject[i]);

                results[i] = status;
            }

            return results;
        }

        /// <summary>
        /// Gets the monitor history.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="monitorInstanceDefinition">The monitor instance definition.</param>
        /// <returns>
        /// The monitor health history for each minute in the provided time range.
        /// true means healthy, false means unhealthy, and null means that the monitor didn't report a health status for that minute.
        /// </returns>
        [Obsolete("We are going to retire this. Please use GetBatchWatchdogHealthHistory in Health SDK instead.")]
        public async Task<TimeSeries<MonitorIdentifier, bool?>> GetMonitorHistoryAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            TimeSeriesDefinition<MonitorIdentifier> monitorInstanceDefinition)
        {
            if (monitorInstanceDefinition == null)
            {
                throw new ArgumentNullException("monitorInstanceDefinition");
            }

            if (startTimeUtc > endTimeUtc)
            {
                throw new ArgumentException(string.Format("startTimeUtc [{0}] must be <= endTimeUtc [{1}]", startTimeUtc, endTimeUtc));
            }

            startTimeUtc = new DateTime(startTimeUtc.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
            endTimeUtc = new DateTime(endTimeUtc.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);

            string dimensionsFlattened = null;

            if (monitorInstanceDefinition.DimensionCombination != null)
            {
                dimensionsFlattened = string.Join(
                    "/",
                    monitorInstanceDefinition.DimensionCombination.Select(
                        d => string.Join("/", SpecialCharsHelper.EscapeTwice(d.Key), SpecialCharsHelper.EscapeTwice(d.Value))));
            }

            string operation = $"{this.HealthRelativeUrl}/history";

            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace/{3}/metric/{4}/monitorId/{5}/from/{6}/to/{7}{8}",
                this.connectionInfo.GetEndpoint(monitorInstanceDefinition.Id.MetricIdentifier.MonitoringAccount),
                operation,
                monitorInstanceDefinition.Id.MetricIdentifier.MonitoringAccount,
                SpecialCharsHelper.EscapeTwice(monitorInstanceDefinition.Id.MetricIdentifier.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(monitorInstanceDefinition.Id.MetricIdentifier.MetricName),
                SpecialCharsHelper.EscapeTwice(monitorInstanceDefinition.Id.MonitorId),
                UnixEpochHelper.GetMillis(startTimeUtc),
                UnixEpochHelper.GetMillis(endTimeUtc),
                dimensionsFlattened != null ? "/" + dimensionsFlattened : string.Empty);

            var response = await this.GetResponseAsStringDelegate(
                new Uri(url),
                HttpMethod.Get,
                this.httpClient,
                monitorInstanceDefinition.Id.MetricIdentifier.MonitoringAccount,
                operation,
                null,
                string.Empty,
                null,
                null,
                MetricQueryResponseDeserializer.CurrentVersion,
                NumAttempts).ConfigureAwait(false);

            var values = JsonConvert.DeserializeObject<List<bool?>>(response.Item1);
            return new TimeSeries<MonitorIdentifier, bool?>(startTimeUtc, endTimeUtc, SerializationConstants.DefaultSeriesResolutionInMinutes, monitorInstanceDefinition, new List<List<bool?>> { values }, TimeSeriesErrorCode.Success);
        }
    }
}
