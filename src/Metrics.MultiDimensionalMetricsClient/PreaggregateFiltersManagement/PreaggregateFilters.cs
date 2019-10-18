// <copyright file="PreaggregateFilters.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

// ReSharper disable once CheckNamespace
namespace Microsoft.Cloud.Metrics.Client.PreaggregateFiltersManagement
{
    using System;
    using System.Collections.Generic;
    using Metrics;
    using Newtonsoft.Json;

    /// <summary>
    /// Represent a set of filters for a single pre-aggregate.
    /// </summary>
    [JsonObject]
    public sealed class PreaggregateFilters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreaggregateFilters"/> class.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="preaggregateDimensionNames">The preaggregate dimension names.</param>
        /// <param name="filterValues">The filter values.</param>
        public PreaggregateFilters(
            string monitoringAccount,
            string metricNamespace,
            string metricName,
            IEnumerable<string> preaggregateDimensionNames,
            IReadOnlyList<DimensionFilter> filterValues)
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

            if (filterValues == null)
            {
                throw new ArgumentNullException(nameof(filterValues));
            }

            if (filterValues.Count == 0)
            {
                throw new ArgumentException($"{nameof(filterValues)} cannot be empty");
            }

            this.MonitoringAccount = monitoringAccount;
            this.MetricNamespace = metricNamespace;
            this.MetricName = metricName;
            var preaggregateDimensionNamesSet = new SortedSet<string>(preaggregateDimensionNames, StringComparer.OrdinalIgnoreCase);

            if (preaggregateDimensionNamesSet.Count == 0)
            {
                throw new ArgumentException($"{nameof(preaggregateDimensionNames)} cannot be empty");
            }

            this.PreaggregateDimensionNames = preaggregateDimensionNamesSet;

            foreach (string dim in this.PreaggregateDimensionNames)
            {
                if (string.IsNullOrWhiteSpace(dim))
                {
                    throw new ArgumentException($"{nameof(preaggregateDimensionNames)} cannot have empty or null values");
                }
            }

            this.DimensionFilters = filterValues;
            var serverRepresentationOfFilterValues = new List<PreaggregateDimensionFilterValues>();
            foreach (DimensionFilter filterValue in filterValues)
            {
                if (filterValue.IsExcludeFilter)
                {
                    throw new ArgumentException($"{nameof(filterValues)} are not allowed to have exclude filters. Dimension Name with exclude filters:{filterValue.DimensionName}");
                }

                serverRepresentationOfFilterValues.Add(new PreaggregateDimensionFilterValues(filterValue.DimensionName, filterValue.DimensionValues));
            }

            this.FilterValues = serverRepresentationOfFilterValues;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreaggregateFilters"/> class.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="preaggregateDimensionNames">The preaggregate dimension names.</param>
        /// <param name="filterValues">The filter values.</param>
        [JsonConstructor]
        internal PreaggregateFilters(
            string monitoringAccount,
            string metricNamespace,
            string metricName,
            IEnumerable<string> preaggregateDimensionNames,
            IReadOnlyList<PreaggregateDimensionFilterValues> filterValues)
        {
            this.MonitoringAccount = monitoringAccount;
            this.MetricNamespace = metricNamespace;
            this.MetricName = metricName;
            var preaggregateDimensionNamesSet = new SortedSet<string>(preaggregateDimensionNames, StringComparer.OrdinalIgnoreCase);
            this.PreaggregateDimensionNames = preaggregateDimensionNamesSet;
            this.FilterValues = filterValues;

            var dimensionFilters = new List<DimensionFilter>(this.FilterValues.Count);
            foreach (PreaggregateDimensionFilterValues filter in this.FilterValues)
            {
                dimensionFilters.Add(DimensionFilter.CreateIncludeFilter(filter.FilterDimensionName, filter.FilterValues));
            }

            this.DimensionFilters = dimensionFilters;
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
        public IEnumerable<string> PreaggregateDimensionNames { get; }

        /// <summary>
        /// Gets the filter values.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<DimensionFilter> DimensionFilters { get; }

        /// <summary>
        /// Gets the filter values representation of server side.
        /// </summary>
        [JsonProperty]
        internal IReadOnlyList<PreaggregateDimensionFilterValues> FilterValues { get; }
    }
}