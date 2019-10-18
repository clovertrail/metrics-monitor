// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KqlMRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// The query request used when the customer queries with a free text query in the KQL-M query language.
    /// </summary>
    internal sealed class KqlMRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KqlMRequest"/> class.
        /// Used for recreation with deserialization.
        /// </summary>
        public KqlMRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KqlMRequest"/> class.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metric">The metric name.</param>
        /// <param name="startTimeUtc">The start time of the query.</param>
        /// <param name="endTimeUtc">The end time of the query.</param>
        /// <param name="queryExpression">The query expression to execute.</param>
        [JsonConstructor]
        public KqlMRequest(
            string monitoringAccount,
            string metricNamespace,
            string metric,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            string queryExpression)
        {
            this.MonitoringAccount = monitoringAccount;
            this.MetricNamespace = metricNamespace;
            this.Metric = metric;
            this.StartTimeUtc = startTimeUtc;
            this.EndTimeUtc = endTimeUtc;
            this.QueryExpression = queryExpression;
        }

        /// <summary>
        /// The monitoring account in the context of the request.
        /// </summary>
        public string MonitoringAccount { get; private set; }

        /// <summary>
        /// The metric namespace in the context of the request.
        /// </summary>
        public string MetricNamespace { get; private set; }

        /// <summary>
        /// The name of the metric in the context of the request.
        /// </summary>
        public string Metric { get; private set; }

        /// <summary>
        /// The start time of the query in UTC time.
        /// </summary>
        public DateTime StartTimeUtc { get; private set; }

        /// <summary>
        /// The end time of the query in UTC time.
        /// </summary>
        public DateTime EndTimeUtc { get; private set; }

        /// <summary>
        /// The query expression to execute.
        /// </summary>
        public string QueryExpression { get; private set; }
    }
}
