// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OboMetricReader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Cloud.Metrics.Client.Query;
    using Microsoft.Cloud.Metrics.Client.Utility;

    /// <summary>
    /// The metrics reader class to read metrics data for OBO V2 - multiple dimension support.
    /// </summary>
    public sealed class OboMetricReader
    {
        private readonly ConnectionInfo connectionInfo;
        private readonly HttpClient httpClient;
        private readonly string clientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="OboMetricReader"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="clientId">The string identifying client.</param>
        public OboMetricReader(ConnectionInfo connectionInfo, string clientId = "OBO")
        {
            if (connectionInfo == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            this.connectionInfo = connectionInfo;
            this.clientId = clientId;
            this.httpClient = HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo);
        }

        /// <summary>
        /// Gets the filtered time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="numMinutes">The number minutes.</param>
        /// <param name="resourceId">The resource identifier.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="categories">The categories.</param>
        /// <returns>List of <see cref="IFilteredTimeSeriesQueryResponse"/>.</returns>
        public async Task<IReadOnlyList<IFilteredTimeSeriesQueryResponse>> GetFilteredTimeSeriesAsync(DateTime startTimeUtc, int numMinutes, string resourceId, SamplingType[] samplingTypes, List<string> categories)
        {
            var startMinute = startTimeUtc.ToString("yyyy-MM-ddTHH:mmZ");
            var endpoint = new Uri(this.connectionInfo.Endpoint, $"/api/getMetricsForOBO/v2/serializationVersion/{FilteredTimeSeriesQueryResponse.CurrentVersion}/startMinute/{startMinute}/numMinutes/{numMinutes}");

            var traceId = Guid.NewGuid();
            var httpContent = Tuple.Create(new List<string> { resourceId }, samplingTypes, categories);

            var response = await HttpClientHelper.GetResponse(
                endpoint,
                HttpMethod.Post,
                this.httpClient,
                null, // TODO add support of monitoring account on server side and then pass it here
                null, // TODO add support of monitoring account on server side and pass operation here
                httpContent,
                traceId: traceId,
                clientId: this.clientId,
                numAttempts: 1).ConfigureAwait(false);

            string handlingRpServerId;
            IReadOnlyList<IFilteredTimeSeriesQueryResponse> results;
            using (HttpResponseMessage httpResponseMessage = response.Item2)
            {
                IEnumerable<string> handlingRpServerIdValues;
                httpResponseMessage.Headers.TryGetValues("__HandlingRpServerId__", out handlingRpServerIdValues);
                handlingRpServerId = handlingRpServerIdValues?.FirstOrDefault();

                using (var stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    results = FilteredQueryResponseDeserializer.Deserialize(stream);
                }
            }

            foreach (var queryResponse in results)
            {
                queryResponse.DiagnosticInfo.TraceId = traceId.ToString("B");
                queryResponse.DiagnosticInfo.HandlingServerId = handlingRpServerId;
            }

            return results;
        }
    }
}
