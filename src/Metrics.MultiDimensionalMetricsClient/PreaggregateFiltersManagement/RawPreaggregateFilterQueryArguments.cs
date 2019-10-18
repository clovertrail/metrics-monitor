// <copyright file="RawPreaggregateFilterQueryArguments.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

// ReSharper disable once CheckNamespace
namespace Microsoft.Cloud.Metrics.Client.PreaggregateFiltersManagement
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the arguments for retrieving raw preaggregate filters.
    /// </summary>
    [JsonObject]
    internal sealed class RawPreaggregateFilterQueryArguments
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RawPreaggregateFilterQueryArguments"/> class.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="preaggregateDimensionNames">The preaggregate dimension names.</param>
        /// <param name="count">The count of filters requested. Use 0 to denote all filters to be returned.</param>
        /// <param name="offset">The offset of the requested filters page calculated based on the count of data returned.</param>
        [JsonConstructor]
        public RawPreaggregateFilterQueryArguments(
            string monitoringAccount,
            string metricNamespace,
            string metricName,
            IEnumerable<string> preaggregateDimensionNames,
            int count,
            int offset)
        {
            if (string.IsNullOrEmpty(monitoringAccount))
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrEmpty(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            if (string.IsNullOrEmpty(metricName))
            {
                throw new ArgumentNullException(nameof(metricName));
            }

            if (preaggregateDimensionNames == null)
            {
                throw new ArgumentNullException(nameof(preaggregateDimensionNames));
            }

            if (count < 0)
            {
                throw new ArgumentException($"{nameof(count)} cannot be negative number");
            }

            if (offset < 0)
            {
                throw new ArgumentException($"{nameof(offset)} cannot be negative number");
            }

            this.MonitoringAccount = monitoringAccount;
            this.MetricNamespace = metricNamespace;
            this.MetricName = metricName;
            this.PreaggregateDimensionNames = new SortedSet<string>(preaggregateDimensionNames, StringComparer.OrdinalIgnoreCase);

            foreach (string dim in this.PreaggregateDimensionNames)
            {
                if (string.IsNullOrEmpty(dim))
                {
                    throw new ArgumentException($"{nameof(preaggregateDimensionNames)} cannot have empty of null values");
                }
            }

            this.Count = count;
            this.Offset = offset;
        }

        /// <summary>
        /// Gets or sets the monitoring account.
        /// </summary>
        public string MonitoringAccount { get; }

        /// <summary>
        /// Gets or sets the metric namespace.
        /// </summary>
        public string MetricNamespace { get; }

        /// <summary>
        /// Gets or sets the name of the metric.
        /// </summary>
        public string MetricName { get; }

        /// <summary>
        /// Gets or sets the preaggregate dimension names.
        /// </summary>
        public SortedSet<string> PreaggregateDimensionNames { get; }

        /// <summary>
        /// Gets the count of filters requested.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the offset of the requested filters page calculated based on the count of data returned.
        /// </summary>
        public int Offset { get; }
    }
}