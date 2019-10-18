// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricReader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Cloud.Metrics.Client.Query;
    using Microsoft.Cloud.Metrics.Client.Utility;
    using Microsoft.Online.Metrics.Serialization;
    using Microsoft.Online.Metrics.Serialization.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The metrics reader class to read metrics data as well as metrics metadata.
    /// </summary>
    public sealed class MetricReader : IMetricReader
    {
        /// <summary>
        /// When a dimension has this value, it is not a pre-condition but it is part of the pre-aggregation.
        /// </summary>
        public const string ContextualHintingWildcardValue = "{{*}}";

#pragma warning disable SA1401 // Fields must be private
        /// <summary>
        /// The relative URL for metrics data
        /// </summary>
        public readonly string DataRelativeUrl;

        /// <summary>
        /// The relative URL for metrics meta-data a.k.a. hinting data
        /// </summary>
        public readonly string MetaDataRelativeUrl;

        /// <summary>
        /// The relative URL for metrics meta-data a.k.a. hinting data V2.
        /// </summary>
        public readonly string MetaDataRelativeUrlV2;

        /// <summary>
        /// The relative Url for distributed query.
        /// </summary>
        public readonly string DistributedQueryRelativeUrl;

        /// <summary>
        /// The query service relative URL.
        /// </summary>
        public readonly string QueryServiceRelativeUrl;
#pragma warning restore SA1401 // Fields must be private

        private const int MillisecondsPerMinute = 60000;

        /// <summary>
        /// The empty string array.
        /// </summary>
        private static readonly string[] EmptyStringArray = new string[0];

        /// <summary>
        /// The empty pre-aggregate configurations.
        /// </summary>
        private static readonly List<PreAggregateConfiguration> EmptyPreAggregateConfigurations = new List<PreAggregateConfiguration>();

        /// <summary>
        /// The HTTP client instance.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// The connection information.
        /// </summary>
        private readonly ConnectionInfo connectionInfo;

        /// <summary>
        /// The metric configuration manager.
        /// </summary>
        private readonly MetricConfigurationManager metricConfigurationManager;

        /// <summary>
        /// The string identifying client.
        /// </summary>
        private readonly string clientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricReader"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="clientId">The string identifying client.</param>
        public MetricReader(ConnectionInfo connectionInfo, string clientId = "ClientAPI")
            : this(connectionInfo, HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo), clientId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricReader"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="authHeaderValue">The auth header.</param>
        /// <param name="clientId">The string identifying client.</param>
        public MetricReader(ConnectionInfo connectionInfo, string authHeaderValue, string clientId = "ClientAPI")
            : this(connectionInfo, HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo, authHeaderValue), clientId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricReader"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="httpClient">The http client with auth info.</param>
        /// <param name="clientId">The string identifying client.</param>
        internal MetricReader(ConnectionInfo connectionInfo, HttpClient httpClient, string clientId = "ClientAPI")
        {
            if (connectionInfo == null)
            {
                throw new ArgumentNullException("connectionInfo");
            }

            this.connectionInfo = connectionInfo;
            this.DataRelativeUrl = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.DataRelativeUrl);
            this.MetaDataRelativeUrl = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.MetaDataRelativeUrl);
            this.MetaDataRelativeUrlV2 = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.MetaDataRelativeUrlV2);
            this.DistributedQueryRelativeUrl = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.DistributedQueryRelativeUrl);
            this.QueryServiceRelativeUrl = this.connectionInfo.GetAuthRelativeUrl(MetricsServerRelativeUris.QueryServiceRelativeUrl);
            this.clientId = clientId;
            this.httpClient = httpClient;
            this.metricConfigurationManager = new MetricConfigurationManager(this.connectionInfo);
        }

        /// <summary>
        /// Gets the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="definition">The time series definition.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definition"/>.
        /// </returns>
        public async Task<TimeSeries<MetricIdentifier, double?>> GetTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            TimeSeriesDefinition<MetricIdentifier> definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }

            var series = await this.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, samplingType, definition).ConfigureAwait(false);
            return series.First();
        }

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions" />.
        /// </returns>
        public Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            params TimeSeriesDefinition<MetricIdentifier>[] definitions)
        {
            return this.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, samplingType, definitions.AsEnumerable());
        }

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions" />.
        /// </returns>
        public Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            IEnumerable<TimeSeriesDefinition<MetricIdentifier>> definitions)
        {
            return this.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, new[] { samplingType }, definitions);
        }

        /// <summary>
        /// Gets the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="definition">The time series definition.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definition"/>.
        /// </returns>
        /// <remarks>This API uses <see cref="AggregationType.Automatic"/> by default and other overloads are available for specific <see cref="AggregationType"/>.</remarks>
        public async Task<TimeSeries<MetricIdentifier, double?>> GetTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            int seriesResolutionInMinutes,
            TimeSeriesDefinition<MetricIdentifier> definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }

            var series = await this.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, samplingType, seriesResolutionInMinutes, definition).ConfigureAwait(false);
            return series.First();
        }

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions" />.
        /// </returns>
        /// <remarks>This API uses <see cref="AggregationType.Automatic"/> by default and other overloads are available for specific <see cref="AggregationType"/>.</remarks>
        public Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            int seriesResolutionInMinutes,
            params TimeSeriesDefinition<MetricIdentifier>[] definitions)
        {
            return this.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, samplingType, seriesResolutionInMinutes, definitions.AsEnumerable());
        }

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingType">The sampling type.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions" />.
        /// </returns>
        /// <remarks>This API uses <see cref="AggregationType.Automatic"/> by default and other overloads are available for specific <see cref="AggregationType"/>.</remarks>
        public Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            int seriesResolutionInMinutes,
            IEnumerable<TimeSeriesDefinition<MetricIdentifier>> definitions)
        {
            return this.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, new[] { samplingType }, definitions, seriesResolutionInMinutes);
        }

        /// <summary>
        /// Gets time series with multiple sampling types.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="definition">The time series definition.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="aggregationType">The aggregation function used to reduce the resolution of the returned series.</param>
        /// <returns>
        /// The time series for the given <paramref name="definition" />.
        /// </returns>
        public async Task<TimeSeries<MetricIdentifier, double?>> GetTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType[] samplingTypes,
            TimeSeriesDefinition<MetricIdentifier> definition,
            int seriesResolutionInMinutes = 1,
            AggregationType aggregationType = AggregationType.Automatic)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var series = await this.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, samplingTypes, new[] { definition }, seriesResolutionInMinutes, aggregationType).ConfigureAwait(false);
            return series.FirstOrDefault();
        }

        /// <summary>
        /// Gets a list of the time series, each with multiple sampling types.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="aggregationType">The aggregation function used to reduce the resolution of the returned series.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions" />.
        /// </returns>
        public async Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType[] samplingTypes,
            IEnumerable<TimeSeriesDefinition<MetricIdentifier>> definitions,
            int seriesResolutionInMinutes = 1,
            AggregationType aggregationType = AggregationType.Automatic)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException("definitions");
            }

            if (samplingTypes == null || samplingTypes.Length == 0)
            {
                throw new ArgumentException("cannot be null or empty", nameof(samplingTypes));
            }

            if (seriesResolutionInMinutes < SerializationConstants.DefaultSeriesResolutionInMinutes)
            {
                throw new ArgumentException($"{seriesResolutionInMinutes} must be >= {SerializationConstants.DefaultSeriesResolutionInMinutes}", nameof(samplingTypes));
            }

            List<TimeSeriesDefinition<MetricIdentifier>> definitionList = definitions.ToList();

            if (definitionList.Count == 0)
            {
                throw new ArgumentException("The count of 'definitions' is 0.");
            }

            if (definitionList.Any(d => d == null))
            {
                throw new ArgumentException("At least one of definitions are null.");
            }

            if (startTimeUtc > endTimeUtc)
            {
                throw new ArgumentException(string.Format("startTimeUtc [{0}] must be <= endTimeUtc [{1}]", startTimeUtc, endTimeUtc));
            }

            NormalizeTimeRange(ref startTimeUtc, ref endTimeUtc);

            foreach (var timeSeriesDefinition in definitionList)
            {
                timeSeriesDefinition.SamplingTypes = samplingTypes;
                timeSeriesDefinition.StartTimeUtc = startTimeUtc;
                timeSeriesDefinition.EndTimeUtc = endTimeUtc;
                timeSeriesDefinition.SeriesResolutionInMinutes = seriesResolutionInMinutes;
                timeSeriesDefinition.AggregationType = aggregationType;
            }

            return await this.GetMultipleTimeSeriesAsync(definitionList).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions"/>.
        /// </returns>
        /// <exception cref="MetricsClientException">
        /// This exception is thrown on all failures in communication.
        /// One should look at inner exception for more details.
        /// </exception>
        public async Task<IReadOnlyList<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(IList<TimeSeriesDefinition<MetricIdentifier>> definitions)
        {
            using (var response = await this.GetMultipleTimeSeriesAsync(definitions, MetricQueryResponseDeserializer.CurrentVersion, returnMetricNames: false).ConfigureAwait(false))
            {
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    return MetricQueryResponseDeserializer.Deserialize(stream, definitions.ToArray()).Item2;
                }
            }
        }

        /// <summary>
        /// Gets the list of namespaces for the <paramref name="monitoringAccount"/>.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <returns>The list of namespaces for the <paramref name="monitoringAccount"/>.</returns>
        public async Task<IReadOnlyList<string>> GetNamespacesAsync(string monitoringAccount)
        {
            if (string.IsNullOrWhiteSpace(monitoringAccount))
            {
                throw new ArgumentException("monitoringAccount is null or empty.");
            }

            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace",
                this.connectionInfo.GetEndpoint(monitoringAccount),
                this.MetaDataRelativeUrl,
                monitoringAccount);

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Get,
                this.httpClient,
                monitoringAccount,
                this.MetaDataRelativeUrl,
                null,
                this.clientId).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<string[]>(response.Item1);
        }

        /// <summary>
        /// Gets the list of metric names for the <paramref name="monitoringAccount" /> and <paramref name="metricNamespace" />.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>
        /// The list of metric names for the <paramref name="monitoringAccount" /> and <paramref name="metricNamespace" />.
        /// </returns>
        public async Task<IReadOnlyList<string>> GetMetricNamesAsync(string monitoringAccount, string metricNamespace)
        {
            if (string.IsNullOrWhiteSpace(monitoringAccount))
            {
                throw new ArgumentException("monitoringAccount is null or empty.");
            }

            if (string.IsNullOrWhiteSpace(metricNamespace))
            {
                throw new ArgumentException("metricNamespace is null or empty.");
            }

            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace/{3}/metric",
                this.connectionInfo.GetEndpoint(monitoringAccount),
                this.MetaDataRelativeUrl,
                monitoringAccount,
                SpecialCharsHelper.EscapeTwice(metricNamespace));

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Get,
                this.httpClient,
                monitoringAccount,
                this.MetaDataRelativeUrl,
                null,
                this.clientId).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<string[]>(response.Item1);
        }

        /// <summary>
        /// Gets the list of dimension names for the <paramref name="metricId" />.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <returns>
        /// The list of dimension names for the <paramref name="metricId" />.
        /// </returns>
        public async Task<IReadOnlyList<string>> GetDimensionNamesAsync(MetricIdentifier metricId)
        {
            var config = await this.metricConfigurationManager.Get(metricId).ConfigureAwait(false);

            return config.DimensionConfigurations == null ? EmptyStringArray : config.DimensionConfigurations.Select(d => d.Id).ToArray();
        }

        /// <summary>
        /// Gets the list of pre-aggregate configurations for the <paramref name="metricId" />.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <returns>
        /// The list of pre-aggregate configurations for the <paramref name="metricId" />.
        /// </returns>
        public async Task<IReadOnlyList<PreAggregateConfiguration>> GetPreAggregateConfigurationsAsync(MetricIdentifier metricId)
        {
            var config = await this.metricConfigurationManager.Get(metricId).ConfigureAwait(false);

            return config.PreAggregations ?? EmptyPreAggregateConfigurations;
        }

        /// <summary>
        /// Gets the known dimension combinations.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <returns>Time series definitions with  known dimension combinations.</returns>
        public Task<IReadOnlyList<TimeSeriesDefinition<MetricIdentifier>>> GetKnownTimeSeriesDefinitionsAsync(
            MetricIdentifier metricId,
            params DimensionFilter[] dimensionFilters)
        {
            return this.GetKnownTimeSeriesDefinitionsAsync(metricId, dimensionFilters.AsEnumerable());
        }

        /// <summary>
        /// Gets the known dimension combinations.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <returns>Time series definitions with  known dimension combinations.</returns>
        public async Task<IReadOnlyList<TimeSeriesDefinition<MetricIdentifier>>> GetKnownTimeSeriesDefinitionsAsync(
            MetricIdentifier metricId,
            IEnumerable<DimensionFilter> dimensionFilters)
        {
            return await this.GetKnownTimeSeriesDefinitionsAsync(metricId, dimensionFilters, DateTime.MinValue, DateTime.MaxValue).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the known dimension combinations.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <param name="newCombinationsOnly">
        /// If true only combinations which were added into memory in this session of hinting system after fromTimeUtc.
        /// This flag does *not* guarantee that only new combinations will be returned
        /// It is more of a hint to the hinting system to try to give only new combinations in given time range.
        /// </param>
        /// <returns>Time series definitions with  known dimension combinations.</returns>
        public async Task<IReadOnlyList<TimeSeriesDefinition<MetricIdentifier>>> GetKnownTimeSeriesDefinitionsAsync(
            MetricIdentifier metricId,
            IEnumerable<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            bool newCombinationsOnly = false)
        {
            metricId.Validate();

            var dimensionNamesAndConstraints = GetDimensionNamesAndConstraints(dimensionFilters);

            var url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace/{3}/metricName/{4}/startTimeUtcMillis/{5}/endTimeUtcMillis/{6}",
                this.connectionInfo.GetEndpoint(metricId.MonitoringAccount),
                this.MetaDataRelativeUrl,
                SpecialCharsHelper.EscapeTwice(metricId.MonitoringAccount),
                SpecialCharsHelper.EscapeTwice(metricId.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(metricId.MetricName),
                UnixEpochHelper.GetMillis(startTimeUtc),
                UnixEpochHelper.GetMillis(endTimeUtc));

            if (newCombinationsOnly)
            {
                url = string.Format("{0}/newOnly", url);
            }

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Post,
                this.httpClient,
                metricId.MonitoringAccount,
                this.MetaDataRelativeUrl,
                dimensionNamesAndConstraints,
                this.clientId).ConfigureAwait(false);

            var deserializedResponse = JsonConvert.DeserializeObject<Tuple<List<string>, List<List<string>>>>(response.Item1);

            TimeSeriesDefinition<MetricIdentifier>[] results;
            if (deserializedResponse != null && deserializedResponse.Item1 != null && deserializedResponse.Item2 != null)
            {
                results = new TimeSeriesDefinition<MetricIdentifier>[deserializedResponse.Item2.Count];
                var preaggreate = deserializedResponse.Item1;

                for (var index = 0; index < deserializedResponse.Item2.Count; index++)
                {
                    var r = deserializedResponse.Item2[index];
                    var dimensionCombination = new KeyValuePair<string, string>[r.Count];
                    for (var i = 0; i < preaggreate.Count; i++)
                    {
                        dimensionCombination[i] = new KeyValuePair<string, string>(preaggreate[i], r[i]);
                    }

                    results[index] = new TimeSeriesDefinition<MetricIdentifier>(metricId, dimensionCombination);
                }
            }
            else
            {
                results = new TimeSeriesDefinition<MetricIdentifier>[0];
            }

            return results;
        }

        /// <summary>
        /// Gets the dimension values for <paramref name="dimensionName"/> satifying the <paramref name="dimensionFilters"/>
        /// and time range (<paramref name="startTimeUtc"/>, <paramref name="endTimeUtc"/>)
        /// </summary>
        /// <remarks>
        /// Time range resolution is day.
        /// </remarks>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">
        /// The dimension filters representing the pre-aggregate dimensions.
        /// Create an emtpy include filter for dimension with no filter values.
        /// Requested dimension should also be part of this and should be empty.
        /// </param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for <paramref name="dimensionName"/>.</returns>
        public async Task<IReadOnlyList<string>> GetDimensionValuesAsync(
            MetricIdentifier metricId,
            List<DimensionFilter> dimensionFilters,
            string dimensionName,
            DateTime startTimeUtc,
            DateTime endTimeUtc)
        {
            metricId.Validate();

            if (dimensionFilters == null || dimensionFilters.Count == 0)
            {
                throw new ArgumentException("Dimension filters cannot be empty or null");
            }

            dimensionFilters.Sort((item1, item2) => string.Compare(item1.DimensionName, item2.DimensionName, StringComparison.OrdinalIgnoreCase));

            var url = string.Format(
                "{0}{1}/monitoringAccount/{2}/metricNamespace/{3}/metric/{4}/startTimeUtcMillis/{5}/endTimeUtcMillis/{6}/dimension/{7}",
                this.connectionInfo.GetEndpoint(metricId.MonitoringAccount),
                this.MetaDataRelativeUrlV2,
                SpecialCharsHelper.EscapeTwice(metricId.MonitoringAccount),
                SpecialCharsHelper.EscapeTwice(metricId.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(metricId.MetricName),
                UnixEpochHelper.GetMillis(startTimeUtc),
                UnixEpochHelper.GetMillis(endTimeUtc),
                SpecialCharsHelper.EscapeTwice(dimensionName));

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Post,
                this.httpClient,
                metricId.MonitoringAccount,
                this.MetaDataRelativeUrlV2,
                null,
                this.clientId,
                JsonConvert.SerializeObject(dimensionFilters)).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<string>>(response.Item1);
        }

        /// <summary>
        /// Gets the known dimension combinations that match the query criteria.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="samplingType">Sampling type to use for this metric.</param>
        /// <param name="reducer">The reducing function to apply to the time series.</param>
        /// <param name="queryFilter">Filter criteria to enforce on the query.</param>
        /// <param name="includeSeries">Indicate whether or not to include the raw time series data in the result.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.</param>
        /// <returns>Time series definitions matching the query criteria.</returns>
        [Obsolete]
        public async Task<IReadOnlyList<IQueryResult>> GetFilteredDimensionValuesAsync(
            MetricIdentifier metricId,
            IEnumerable<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            Reducer reducer,
            QueryFilter queryFilter,
            bool includeSeries,
            SelectionClause selectionClause = null,
            AggregationType aggregationType = AggregationType.Sum,
            long seriesResolutionInMinutes = 1)
        {
            SortedDictionary<string, Tuple<bool, IReadOnlyList<string>>> dimensionNamesAndConstraints;
            var query = this.BuildQueryParameters(metricId, dimensionFilters, startTimeUtc, endTimeUtc, samplingType, reducer, queryFilter, includeSeries, selectionClause, aggregationType, seriesResolutionInMinutes, out dimensionNamesAndConstraints);

            string path = string.Format(
                "{0}/monitoringAccount/{1}/metricNamespace/{2}/metric/{3}",
                this.DistributedQueryRelativeUrl,
                SpecialCharsHelper.EscapeTwice(metricId.MonitoringAccount),
                SpecialCharsHelper.EscapeTwice(metricId.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(metricId.MetricName));

            var builder = new UriBuilder(this.connectionInfo.GetEndpoint(metricId.MonitoringAccount))
            {
                Path = path,
                Query = query.ToString()
            };

            var response = await HttpClientHelper.GetResponse(
                builder.Uri,
                HttpMethod.Post,
                this.httpClient,
                metricId.MonitoringAccount,
                this.DistributedQueryRelativeUrl,
                dimensionNamesAndConstraints,
                this.clientId).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<QueryResult[]>(response.Item1);
        }

        /// <summary>
        /// Gets the known dimension combinations that match the query criteria.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="samplingType">Sampling type to use for this metric.</param>
        /// <param name="reducer">The reducing function to apply to the time series.</param>
        /// <param name="queryFilter">Filter criteria to enforce on the query.</param>
        /// <param name="includeSeries">Indicate whether or not to include the raw time series data in the result.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.</param>
        /// <returns>Time series definitions matching the query criteria.</returns>
        [Obsolete]
        public async Task<QueryResultsList> GetFilteredDimensionValuesAsyncV2(
            MetricIdentifier metricId,
            IEnumerable<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            Reducer reducer,
            QueryFilter queryFilter,
            bool includeSeries,
            SelectionClause selectionClause = null,
            AggregationType aggregationType = AggregationType.Sum,
            long seriesResolutionInMinutes = 1)
        {
            SortedDictionary<string, Tuple<bool, IReadOnlyList<string>>> dimensionNamesAndConstraints;
            var query = this.BuildQueryParameters(metricId, dimensionFilters, startTimeUtc, endTimeUtc, samplingType, reducer, queryFilter, includeSeries, selectionClause, aggregationType, seriesResolutionInMinutes, out dimensionNamesAndConstraints);

            string operation = $"{this.DistributedQueryRelativeUrl}/V2";

            string path = string.Format(
                "{0}/monitoringAccount/{1}/metricNamespace/{2}/metric/{3}",
                operation,
                SpecialCharsHelper.EscapeTwice(metricId.MonitoringAccount),
                SpecialCharsHelper.EscapeTwice(metricId.MetricNamespace),
                SpecialCharsHelper.EscapeTwice(metricId.MetricName));

            var builder = new UriBuilder(this.connectionInfo.GetEndpoint(metricId.MonitoringAccount))
            {
                Path = path,
                Query = query.ToString()
            };

            var response = await HttpClientHelper.GetResponse(
                builder.Uri,
                HttpMethod.Post,
                this.httpClient,
                metricId.MonitoringAccount,
                operation,
                dimensionNamesAndConstraints,
                this.clientId).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<QueryResultsList>(response.Item1);
        }

        /// <summary>
        /// Gets the time series values that match the filtering criteria.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.  By default, all results are returned.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.  By default, automatic resolution is used (same as Jarvis UI).</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.  1 minute resolution (full resolution in MDM today) by default.</param>
        /// <param name="traceId">The trace identifier for the query, used for diagnostic purposes only.  If a trace id is not provided, one will be generated.</param>
        /// <param name="outputDimensionNames">The output dimension names.</param>
        /// <param name="lastValueMode">Indicating if the query should be fulfilled with last value mode. If true, null values in the query range requested will be filled with the last known value.</param>
        /// <returns>
        /// Time series definitions matching the query criteria.
        /// </returns>
        [Obsolete]
        public async Task<IQueryResultListV3> GetFilteredDimensionValuesAsyncV3(
            MetricIdentifier metricId,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            IReadOnlyList<SamplingType> samplingTypes,
            SelectionClauseV3 selectionClause = null,
            AggregationType aggregationType = AggregationType.Automatic,
            long seriesResolutionInMinutes = 1,
            Guid? traceId = null,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false)
        {
            if (samplingTypes == null || samplingTypes.Count == 0)
            {
                throw new ArgumentException("One or more sampling types must be specified.");
            }

            if (selectionClause == null)
            {
                selectionClause = new SelectionClauseV3(new PropertyDefinition(PropertyAggregationType.Average, samplingTypes[0]), int.MaxValue, OrderBy.Undefined);
            }

            if (dimensionFilters == null)
            {
                throw new ArgumentNullException(nameof(dimensionFilters));
            }

            if (startTimeUtc > endTimeUtc)
            {
                throw new ArgumentException("Start time must be before end time.");
            }

            traceId = traceId ?? Guid.NewGuid();

            var request = new FilteredTimeSeriesQueryRequest(
                metricId,
                samplingTypes,
                dimensionFilters,
                startTimeUtc,
                endTimeUtc,
                (int)seriesResolutionInMinutes,
                aggregationType,
                selectionClause.PropertyDefinition,
                selectionClause.NumberOfResultsToReturn,
                selectionClause.OrderBy,
                false,
                outputDimensionNames,
                lastValueMode);

            var url = string.Format(
                "{0}{1}/v1/multiple/serializationVersion/{2}/maxCost/{3}?timeoutInSeconds={4}&returnRequestObjectOnFailure={5}",
                this.connectionInfo.GetMetricsDataQueryEndpoint(request.MetricIdentifier.MonitoringAccount).OriginalString,
                this.QueryServiceRelativeUrl,
                FilteredTimeSeriesQueryResponse.CurrentVersion,
                int.MaxValue,
                (int)this.connectionInfo.Timeout.TotalSeconds,
                false);

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Post,
                this.httpClient,
                null, // TODO add support of monitoring account on server side and then pass it here
                null, // TODO add support of monitoring account on server side and pass operation here
                new[] { request },
                this.clientId,
                null,
                traceId,
                FilteredTimeSeriesQueryResponse.CurrentVersion).ConfigureAwait(false);

            using (HttpResponseMessage httpResponseMessage = response.Item2)
            {
                if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                {
                    throw new MetricsClientException($"Request failed with HTTP Status Code: {httpResponseMessage.StatusCode}. TraceId: {traceId}.  Response: {response.Item1}");
                }

                IReadOnlyList<IFilteredTimeSeriesQueryResponse> results;
                using (var stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    results = FilteredQueryResponseDeserializer.Deserialize(stream);
                }

                if (results == null || results.Count == 0)
                {
                    throw new MetricsClientException($"Response is null or empty.  TraceId: {traceId}");
                }

                IFilteredTimeSeriesQueryResponse result = results[0];
                if (result.ErrorCode != FilteredTimeSeriesQueryResponseErrorCode.Success)
                {
                    throw new MetricsClientException(
                        $"Error occured processing the request.  Error code: {result.ErrorCode}. {result.DiagnosticInfo}",
                        null,
                        traceId.Value,
                        httpResponseMessage.StatusCode);
                }

                return new QueryResultListV3(result.StartTimeUtc, result.EndTimeUtc, result.TimeResolutionInMinutes, (IReadOnlyList<FilteredTimeSeries>)result.FilteredTimeSeriesList);
            }
        }

        /// <summary>
        /// Gets the time series values that match the filtering criteria.
        /// This version of code gets values one by one and is useful for scenarios where you do not want to
        /// keep the list of the results in memory and process them on the fly.
        /// Note: This API is for advanced scenarios only. Use it only when you fetch huge amounts of metrics from
        /// multiple stamps in parallel and face performance problems related to memory usage
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.  By default, all results are returned.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.  By default, automatic resolution is used (same as Jarvis UI).</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.  1 minute resolution (full resolution in MDM today) by default.</param>
        /// <param name="traceId">The trace identifier for the query, used for diagnostic purposes only.  If a trace id is not provided, one will be generated.</param>
        /// <param name="outputDimensionNames">The output dimension names.</param>
        /// <returns>
        /// The response message.
        /// </returns>
        public async Task<HttpResponseMessage> GetTimeSeriesStreamedAsync(
            MetricIdentifier metricId,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            IReadOnlyList<SamplingType> samplingTypes,
            SelectionClauseV3 selectionClause = null,
            AggregationType aggregationType = AggregationType.Automatic,
            long seriesResolutionInMinutes = 1,
            Guid? traceId = null,
            IReadOnlyList<string> outputDimensionNames = null)
        {
            if (samplingTypes == null || samplingTypes.Count == 0)
            {
                throw new ArgumentException("One or more sampling types must be specified.");
            }

            if (selectionClause == null)
            {
                selectionClause = new SelectionClauseV3(new PropertyDefinition(PropertyAggregationType.Average, samplingTypes[0]), int.MaxValue, OrderBy.Undefined);
            }

            if (dimensionFilters == null)
            {
                throw new ArgumentNullException(nameof(dimensionFilters));
            }

            if (startTimeUtc > endTimeUtc)
            {
                throw new ArgumentException("Start time must be before end time.");
            }

            traceId = traceId ?? Guid.NewGuid();

            var request = new FilteredTimeSeriesQueryRequest(
                metricId,
                samplingTypes,
                dimensionFilters,
                startTimeUtc,
                endTimeUtc,
                (int)seriesResolutionInMinutes,
                aggregationType,
                selectionClause.PropertyDefinition,
                selectionClause.NumberOfResultsToReturn,
                selectionClause.OrderBy,
                false,
                outputDimensionNames);

            var url = string.Format(
                "{0}{1}/v1/multiple/serializationVersion/{2}/maxCost/{3}?timeoutInSeconds={4}&returnRequestObjectOnFailure={5}",
                this.connectionInfo.GetMetricsDataQueryEndpoint(request.MetricIdentifier.MonitoringAccount).OriginalString,
                this.QueryServiceRelativeUrl,
                FilteredTimeSeriesQueryResponse.CurrentVersion,
                int.MaxValue,
                (int)this.connectionInfo.Timeout.TotalSeconds,
                false);

            Tuple<string, HttpResponseMessage> response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Post,
                this.httpClient,
                null, // TODO add support of monitoring account on server side and then pass it here
                null, // TODO add support of monitoring account on server side and pass operation here
                new[] { request },
                this.clientId,
                null,
                traceId,
                FilteredTimeSeriesQueryResponse.CurrentVersion).ConfigureAwait(false);

            HttpResponseMessage httpResponseMessage = response.Item2;

            if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                throw new MetricsClientException($"Request failed with HTTP Status Code: {httpResponseMessage.StatusCode}. TraceId: {traceId}.  Response: {response.Item1}");
            }

            return httpResponseMessage;
        }

        /// <summary>
        /// Executes the given Kql-M query and returns results as a datatable.
        /// Datatable is represented by JArray where each element represents a row in the query result.
        ///
        /// For example:
        /// Input Query for last 1 hour at 1 minute resolution:
        /// metricNamespace("Metrics.Server").metric("ClientAggregatedMetricCount").dimensions("Datacenter") | project Average
        /// Output (will contain one row for each datapoint):
        /// [
        ///   {
        ///         "TimestampUtc": "02/04/2019 07:11:00",
        ///         "AccountName": "MetricTeamInternalMetrics",
        ///         "MetricNamespace": "Metrics.Server",
        ///         "MetricName": "ClientAggregatedMetricCount",
        ///         "Datacenter": "EastUS2",
        ///         "Average": 70.083851254134714
        ///    },
        ///    {
        ///         "TimestampUtc": "02/04/2019 07:12:00",
        ///         "AccountName": "MetricTeamInternalMetrics",
        ///         "MetricNamespace": "Metrics.Server",
        ///         "MetricName": "ClientAggregatedMetricCount",
        ///         "Datacenter": "EastUS2",
        ///         "Average": 67.305346411549351
        ///     }
        /// ]
        /// </summary>
        /// <param name="traceId">Trace id for end to end tracing purposes.</param>
        /// <param name="accountName">Account name for which query needs to be run.</param>
        /// <param name="queryString">KQL-M query.</param>
        /// <param name="startTimeUtc">Start time utc for the query.</param>
        /// <param name="endTimeUtc">End time utc for the query.</param>
        /// <returns>Result for query.</returns>
        public async Task<JArray> ExecuteKqlMQueryAsync(
            string traceId,
            string accountName,
            string queryString,
            DateTime startTimeUtc,
            DateTime endTimeUtc)
        {
            var queryLanguageRequest =
                new KqlMRequest(accountName, "[N/A]", "[N/A]", startTimeUtc, endTimeUtc, queryString);

            var url = string.Format(
                "{0}{1}query/v2/language/monitoringAccount/{2}",
                this.connectionInfo.GetEndpoint(accountName).OriginalString,
                this.connectionInfo.GetAuthRelativeUrl(string.Empty),
                accountName);

            Guid traceIdGuid;
            Guid? traceIdNullable = null;
            if (Guid.TryParse(traceId, out traceIdGuid))
            {
                traceIdNullable = traceIdGuid;
            }

            var response = await HttpClientHelper.GetResponse(
                url: new Uri(url),
                method: HttpMethod.Post,
                client: this.httpClient,
                monitoringAccount: accountName,
                operation: null,
                httpContent: queryLanguageRequest,
                clientId: this.clientId,
                serializedContent: null,
                traceId: traceIdNullable,
                serializationVersion: FilteredTimeSeriesQueryResponse.CurrentVersion).ConfigureAwait(false);

            using (HttpResponseMessage httpResponseMessage = response.Item2)
            {
                if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                {
                    throw new MetricsClientException($"Request failed with HTTP Status Code: {httpResponseMessage.StatusCode}. TraceId: {traceId}.  Response: {response.Item1}");
                }

                using (var stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    return QueryLanguageResponseToDatatable.GetResponseAsTable(stream);
                }
            }
        }

        /// <summary>
        /// Gets the time series values that match the filtering criteria.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.  By default, all results are returned.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.  By default, automatic resolution is used (same as Jarvis UI).</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.  1 minute resolution (full resolution in MDM today) by default.</param>
        /// <param name="traceId">The trace identifier for the query, used for diagnostic purposes only.  If a trace id is not provided, one will be generated.</param>
        /// <param name="outputDimensionNames">The output dimension names.</param>
        /// <param name="lastValueMode">Indicating if the query should be fulfilled with last value mode. If true, null values in the query range requested will be filled with the last known value.</param>
        /// <returns>
        /// Time series results matching the query criteria.
        /// </returns>
        public Task<IQueryResultListV3> GetTimeSeriesAsync(
            MetricIdentifier metricId,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            IReadOnlyList<SamplingType> samplingTypes,
            SelectionClauseV3 selectionClause = null,
            AggregationType aggregationType = AggregationType.Automatic,
            long seriesResolutionInMinutes = 1,
            Guid? traceId = null,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false)
        {
            return this.GetFilteredDimensionValuesAsyncV3(
                metricId,
                dimensionFilters,
                startTimeUtc,
                endTimeUtc,
                samplingTypes,
                selectionClause,
                aggregationType,
                seriesResolutionInMinutes,
                traceId,
                outputDimensionNames,
                lastValueMode);
        }

        /// <summary>
        /// Returns all metric definitions for time series satisfying given set of filters in the given monitoring account.
        /// </summary>
        /// <remarks>
        /// QOS metrics, composite metrics and wild card metrics are not included in the result set.
        /// </remarks>
        /// <param name="monitoringAccount">Monitoring account name.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="traceId">The trace identifier for the query, used for diagnostic purposes only.  If a trace id is not provided, one will be generated.</param>
        /// <returns>
        /// All metric definitions which has data for time series keys satisfy the given set of filters.
        /// </returns>
        public async Task<IReadOnlyList<MetricDefinitionV2>> GetMetricDefinitionsAsync(
            string monitoringAccount,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            Guid? traceId = null)
        {
            if (string.IsNullOrEmpty(monitoringAccount))
            {
                throw new ArgumentNullException(nameof(monitoringAccount), "Monitoring account cannot be null or empty");
            }

            if (dimensionFilters == null || dimensionFilters.Count == 0)
            {
                throw new ArgumentNullException(nameof(dimensionFilters), "Dimension filters cannot be null or empty");
            }

            if (startTimeUtc > endTimeUtc)
            {
                throw new ArgumentException(string.Format("Start time cannot be greater than end time. StartTime:{0}, EndTime:{1}", startTimeUtc, endTimeUtc));
            }

            bool hasAtleastOneDimensionValueFilter = false;
            foreach (var dimensionFilter in dimensionFilters)
            {
                if (dimensionFilter.DimensionValues.Count > 0)
                {
                    hasAtleastOneDimensionValueFilter = true;
                    break;
                }
            }

            if (!hasAtleastOneDimensionValueFilter)
            {
                throw new ArgumentException("Dimension filters need to have atleast one dimension with dimension values filters");
            }

            var dimensionNamesAndConstraints = GetDimensionNamesAndConstraints(dimensionFilters);

            var url = string.Format(
                "{0}{1}/metricDefinitions/monitoringAccount/{2}/startTimeUtcMillis/{3}/endTimeUtcMillis/{4}",
                this.connectionInfo.GetEndpoint(monitoringAccount),
                this.MetaDataRelativeUrl,
                SpecialCharsHelper.EscapeTwice(monitoringAccount),
                UnixEpochHelper.GetMillis(startTimeUtc),
                UnixEpochHelper.GetMillis(endTimeUtc));

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Post,
                this.httpClient,
                monitoringAccount,
                this.MetaDataRelativeUrl,
                dimensionNamesAndConstraints,
                this.clientId,
                traceId: traceId).ConfigureAwait(false);

            var metricDefinitionsAsByteArray = await response.Item2.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            using (var ms = new MemoryStream(metricDefinitionsAsByteArray))
            {
                using (var reader = new BinaryReader(ms, Encoding.UTF8))
                {
                    reader.ReadByte();

                    List<string> tempList = new List<string>();
                    int numberOfMetadata = reader.ReadInt32();
                    var result = new List<MetricDefinitionV2>(numberOfMetadata);
                    for (int i = 0; i < numberOfMetadata; i++)
                    {
                        var monitoringAcct = reader.ReadString();
                        var metricNamespace = reader.ReadString();
                        var metricName = reader.ReadString();

                        var numOfDimensions = reader.ReadInt32();
                        for (int j = 0; j < numOfDimensions; j++)
                        {
                            tempList.Add(reader.ReadString());
                        }

                        var metadata = new MetricDefinitionV2(monitoringAcct, metricNamespace, metricName, tempList);
                        result.Add(metadata);
                        tempList.Clear();
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="definitions">The time series definitions.</param>
        /// <param name="serializationVersion">The serialization version.</param>
        /// <param name="returnMetricNames">if set to <c>true</c>, return metric names in response.</param>
        /// <param name="traceId">The trace identifier.</param>
        /// <returns>
        /// The time series of for the given <paramref name="definitions" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">If arguments provided incorrectly</exception>
        /// <exception cref="ArgumentException">
        /// The count of 'definitions' is 0.
        /// or
        /// At least one of definitions is null.
        /// </exception>
        public async Task<HttpResponseMessage> GetMultipleTimeSeriesAsync(
            IEnumerable<TimeSeriesDefinition<MetricIdentifier>> definitions,
            byte serializationVersion = MetricQueryResponseDeserializer.CurrentVersion,
            bool returnMetricNames = false,
            Guid? traceId = null)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var definitionArray = definitions.ToArray();

            if (definitionArray.Length == 0)
            {
                throw new ArgumentException("The count of 'definitions' is 0.");
            }

            if (definitionArray.Any(d => d == null))
            {
                throw new ArgumentException("At least one of definitions is null.");
            }

            string operation = $"{this.DataRelativeUrl}/binary/version/{serializationVersion}";
            string monitoringAccount = definitionArray[0].Id.MonitoringAccount;

            string url = string.Format(
                "{0}{1}/monitoringAccount/{2}/returnMetricNames/{3}",
                this.connectionInfo.GetMetricsDataQueryEndpoint(monitoringAccount).OriginalString,
                operation,
                monitoringAccount,
                returnMetricNames);

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Post,
                this.httpClient,
                monitoringAccount,
                operation,
                definitionArray,
                this.clientId,
                null,
                traceId,
                serializationVersion).ConfigureAwait(false);

            return response.Item2;
        }

        /// <summary>
        /// Gets time series filtered by dimension criteral and a Top N series condition.
        /// </summary>
        /// <param name="filteredQueryRequests">The filtered query requests.</param>
        /// <param name="serializationVersion">The serialization version.</param>
        /// <param name="maximumAllowedQueryCost">The maximum allowed query cost.</param>
        /// <param name="traceId">The trace identifier.</param>
        /// <param name="returnRequestObjectOnFailure">if set to <c>true</c> [return request object on failure].</param>
        /// <returns>
        /// The raw stream of query results.
        /// </returns>
        [Obsolete]
        public async Task<HttpResponseMessage> GetFilteredTimeSeriesAsync(
            IReadOnlyList<FilteredTimeSeriesQueryRequest> filteredQueryRequests,
            byte serializationVersion,
            long maximumAllowedQueryCost,
            Guid traceId,
            bool returnRequestObjectOnFailure)
        {
            if (filteredQueryRequests == null)
            {
                throw new ArgumentNullException(nameof(filteredQueryRequests));
            }

            if (filteredQueryRequests.Count == 0)
            {
                throw new ArgumentException("The count of 'filteredQueryRequests' is 0.");
            }

            if (filteredQueryRequests.Any(d => d == null))
            {
                throw new ArgumentException("At least one of filteredQueryRequests is null.");
            }

            var url = string.Format(
                "{0}{1}/v1/multiple/serializationVersion/{2}/maxCost/{3}?timeoutInSeconds={4}&returnRequestObjectOnFailure={5}",
                this.connectionInfo.GetMetricsDataQueryEndpoint(filteredQueryRequests[0].MetricIdentifier.MonitoringAccount).OriginalString,
                this.QueryServiceRelativeUrl,
                serializationVersion,
                maximumAllowedQueryCost,
                (int)this.connectionInfo.Timeout.TotalSeconds,
                returnRequestObjectOnFailure);

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Post,
                this.httpClient,
                null, // TODO add support of monitoring account on server side and then pass it here
                null, // TODO add support of monitoring account on server side and pass operation here
                filteredQueryRequests,
                this.clientId,
                null,
                traceId,
                serializationVersion).ConfigureAwait(false);

            return response.Item2;
        }

        /// <summary>
        /// Normalizes the time range.
        /// </summary>
        /// <param name="startTimeUtc">The start time in UTC.</param>
        /// <param name="endTimeUtc">The end time in UTC.</param>
        private static void NormalizeTimeRange(ref DateTime startTimeUtc, ref DateTime endTimeUtc)
        {
            startTimeUtc = new DateTime(startTimeUtc.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
            endTimeUtc = new DateTime(endTimeUtc.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
        }

        /// <summary>
        /// Get dimensions names and constraints from dimension filters.
        /// </summary>
        /// <param name="dimensionFilters">The dimension filters.</param>
        /// <returns>Dimension constraint</returns>
        /// <exception cref="MetricsClientException">Only one filter can be specified for a dimension.</exception>
        private static SortedDictionary<string, Tuple<bool, IReadOnlyList<string>>> GetDimensionNamesAndConstraints(IEnumerable<DimensionFilter> dimensionFilters)
        {
            var dimensionNamesAndConstraints =
                new SortedDictionary<string, Tuple<bool, IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase);

            if (dimensionFilters != null)
            {
                foreach (var filter in dimensionFilters)
                {
                    Tuple<bool, IReadOnlyList<string>> tuple;
                    if (filter.DimensionValues == null)
                    {
                        tuple = Tuple.Create(true, (IReadOnlyList<string>)EmptyStringArray);
                    }
                    else
                    {
                        tuple = Tuple.Create(filter.IsExcludeFilter, filter.DimensionValues);
                    }

                    if (dimensionNamesAndConstraints.ContainsKey(filter.DimensionName))
                    {
                        throw new MetricsClientException(
                            "Only one filter can be specified for a dimension. Another filter already exists for dimension: " +
                            filter.DimensionName);
                    }

                    dimensionNamesAndConstraints.Add(filter.DimensionName, tuple);
                }
            }

            return dimensionNamesAndConstraints;
        }

        /// <summary>
        /// Builds the dimension names and constraints for DQ query.
        /// </summary>
        /// <param name="metricId">The metric identifier.</param>
        /// <param name="dimensionFilters">The dimension filters, used to indicate one or more possible dimension values.</param>
        /// <param name="startTimeUtc">Start time for time series.</param>
        /// <param name="endTimeUtc">End time for time series.</param>
        /// <param name="samplingType">Sampling type to use for this metric.</param>
        /// <param name="reducer">The reducing function to apply to the time series.</param>
        /// <param name="queryFilter">Filter criteria to enforce on the query.</param>
        /// <param name="includeSeries">Indicate whether or not to include the raw time series data in the result.</param>
        /// <param name="selectionClause">Reduce result to top N results of the query.</param>
        /// <param name="aggregationType">Aggregation function to use when reducing the resolution of the returned series.</param>
        /// <param name="seriesResolutionInMinutes">Reduce size of included series array by adjusting the resolution.</param>
        /// <param name="dimensionNamesAndConstraints">TODO: Comment</param>
        /// <returns>Time series definitions matching the query criteria.</returns>
        private NameValueCollection BuildQueryParameters(
            MetricIdentifier metricId,
            IEnumerable<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType samplingType,
            Reducer reducer,
            QueryFilter queryFilter,
            bool includeSeries,
            SelectionClause selectionClause,
            AggregationType aggregationType,
            long seriesResolutionInMinutes,
            out SortedDictionary<string, Tuple<bool, IReadOnlyList<string>>> dimensionNamesAndConstraints)
        {
            // There is an internal collection called HttpValueCollection for which the ToString() value
            // correctly generates a query string.  We can access this collection type by calling parse
            // on an empty query string.
            var query = HttpUtility.ParseQueryString(string.Empty);
            var startTimeMillis = UnixEpochHelper.GetMillis(startTimeUtc);
            var endTimeMillis = UnixEpochHelper.GetMillis(endTimeUtc);

            if (startTimeMillis > endTimeMillis)
            {
                throw new ArgumentException("Start time must be before end time.");
            }

            if (queryFilter == null)
            {
                throw new ArgumentNullException("queryFilter");
            }

            if (reducer == Reducer.Undefined)
            {
                throw new ArgumentException("Reducer cannot not be undefined.  Use QueryFilter.NoFilter to get all time series.");
            }

            if (!object.ReferenceEquals(queryFilter, QueryFilter.NoFilter))
            {
                if (queryFilter.Operator == Operator.Undefined)
                {
                    throw new ArgumentException("Operator cannot not be undefined.  Use QueryFilter.NoFilter to get all time series.");
                }
            }

            query["SamplingType"] = samplingType.ToString();
            query["startTime"] = startTimeMillis.ToString();
            query["endTime"] = endTimeMillis.ToString();
            query["includeSeries"] = includeSeries.ToString();
            query["reducer"] = reducer.ToString();

            if (includeSeries)
            {
                query["seriesAggregationType"] = aggregationType.ToString();
                query["seriesResolution"] = (seriesResolutionInMinutes * MillisecondsPerMinute).ToString();
            }

            if (object.ReferenceEquals(queryFilter, QueryFilter.NoFilter))
            {
                query["noFilter"] = "true";
            }
            else
            {
                query["operator"] = queryFilter.Operator.ToString();
                query["operand"] = queryFilter.Operand.ToString(CultureInfo.InvariantCulture);
            }

            if (selectionClause != null && !object.ReferenceEquals(selectionClause, SelectionClause.AllResults))
            {
                query["selectionType"] = selectionClause.SelectionType.ToString();
                query["top"] = selectionClause.QuantityToSelect.ToString();
                query["orderBy"] = selectionClause.OrderBy.ToString();
            }

            dimensionNamesAndConstraints = GetDimensionNamesAndConstraints(dimensionFilters);
            return query;
        }
    }
}
