//-------------------------------------------------------------------------------------------------
// <copyright file="Preaggregation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// A grouping of dimensions used to aggregate metric data.
    /// </summary>
    public sealed class Preaggregation : IPreaggregation
    {
        private readonly List<string> dimensions;
        private string name;
        private IMinMaxConfiguration minMaxConfiguration;
        private IPercentileConfiguration percentileConfiguration;
        private IRollupConfiguration rollupConfiguration;
        private IPublicationConfiguration publicationConfiguration;
        private IDistinctCountConfiguration distinctCountConfiguration;
        private IFilteringConfiguration filteringConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Preaggregation"/> class.
        /// Creates a new preaggregate.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="minMaxConfiguration">The minimum maximum configuration.</param>
        /// <param name="percentileConfiguration">The percentile configuration.</param>
        /// <param name="rollupConfiguration">The rollup configuration.</param>
        /// <param name="publicationConfiguration">The publication configuration.</param>
        /// <param name="distinctCountConfiguration">The distinct count configuration.</param>
        /// <param name="filteringConfiguration">The filtering configuration.</param>
        [JsonConstructor]
        internal Preaggregation(
            string name,
            IEnumerable<string> dimensions,
            MinMaxConfiguration minMaxConfiguration,
            PercentileConfiguration percentileConfiguration,
            RollupConfiguration rollupConfiguration,
            PublicationConfiguration publicationConfiguration,
            DistinctCountConfiguration distinctCountConfiguration,
            IFilteringConfiguration filteringConfiguration)
        {
            this.Name = name;
            this.dimensions = dimensions.ToList();
            this.minMaxConfiguration = minMaxConfiguration;
            this.percentileConfiguration = percentileConfiguration;
            this.rollupConfiguration = rollupConfiguration;
            this.publicationConfiguration = publicationConfiguration;
            this.distinctCountConfiguration = distinctCountConfiguration;
            this.filteringConfiguration = filteringConfiguration;
        }

        /// <summary>
        /// Gets or sets the name of the preaggregate.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.name = value;
            }
        }

        /// <summary>
        /// Gets the dimensions of the preaggregate in sorted order.
        /// </summary>
        public IEnumerable<string> Dimensions
        {
            get { return this.dimensions; }
        }

        /// <summary>
        /// The min/max sampling type configuration.
        /// </summary>
        public IMinMaxConfiguration MinMaxConfiguration
        {
            get
            {
                return this.minMaxConfiguration;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.minMaxConfiguration = value;
            }
        }

        /// <summary>
        /// The filtering type configuration.
        /// </summary>
        public IFilteringConfiguration FilteringConfiguration
        {
            get
            {
                return this.filteringConfiguration;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.filteringConfiguration = value;
            }
        }

        /// <summary>
        /// The percentile sampling type configuration.
        /// </summary>
        public IPercentileConfiguration PercentileConfiguration
        {
            get
            {
                return this.percentileConfiguration;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.percentileConfiguration = value;
            }
        }

        /// <summary>
        /// The data rollup configuration.
        /// </summary>
        public IRollupConfiguration RollupConfiguration
        {
            get
            {
                return this.rollupConfiguration;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.rollupConfiguration = value;
            }
        }

        /// <summary>
        /// The metric data store configuration.
        /// </summary>
        public IPublicationConfiguration PublicationConfiguration
        {
            get
            {
                return this.publicationConfiguration;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.publicationConfiguration = value;
            }
        }

        /// <summary>
        /// The distinct count sampling type configuration.
        /// </summary>
        public IDistinctCountConfiguration DistinctCountConfiguration
        {
            get
            {
                return this.distinctCountConfiguration;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.distinctCountConfiguration = value;
            }
        }

        /// <summary>
        /// Creates a new preaggregate with defaults for the configuration flags.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <returns>The created preaggregate.</returns>
        public static Preaggregation CreatePreaggregation(string name, IEnumerable<string> dimensions)
        {
            return CreatePreaggregationImpl(
                name,
                dimensions,
                Configuration.MinMaxConfiguration.MinMaxDisabled,
                Configuration.PercentileConfiguration.PercentileDisabled,
                Configuration.RollupConfiguration.RollupDisabled,
                Configuration.PublicationConfiguration.CacheServer,
                new DistinctCountConfiguration(),
                Configuration.FilteringConfiguration.FilteringDisabled);
        }

        /// <summary>
        /// Creates a new filtered metrics store preaggregate with defaults for the configuration flags.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="minMaxConfiguration">The minimum maximum configuration. Default value is Configuration.MinMaxConfiguration.MinMaxDisabled.</param>
        /// <param name="percentileConfiguration">The percentile configuration. Default value is Configuration.PercentileConfiguration.PercentileDisabled.</param>
        /// <param name="rollupConfiguration">The rollup configuration. Default value is Configuration.RollupConfiguration.RollupDisabled.</param>
        /// <param name="metricStoreConfiguration">The metric store configuration. Default value is Configuration.PublicationConfiguration.AggregatedMetricsStore.</param>
        /// <param name="distinctCountConfiguration">The distinct count configuration. Default value is new DistinctCountConfiguration().</param>
        /// <param name="filteringConfiguration">The filtering configuration. Default value is Configuration.FilteringConfiguration.FilteringDisabled.</param>
        /// <returns>The created preaggregate.</returns>
        public static Preaggregation CreatePreaggregationWithDefaults(
            string name,
            IEnumerable<string> dimensions,
            MinMaxConfiguration minMaxConfiguration = null,
            PercentileConfiguration percentileConfiguration = null,
            RollupConfiguration rollupConfiguration = null,
            PublicationConfiguration metricStoreConfiguration = null,
            DistinctCountConfiguration distinctCountConfiguration = null,
            IFilteringConfiguration filteringConfiguration = null)
        {
            return CreatePreaggregationImpl(
                name,
                dimensions,
                minMaxConfiguration ?? Configuration.MinMaxConfiguration.MinMaxDisabled,
                percentileConfiguration ?? Configuration.PercentileConfiguration.PercentileDisabled,
                rollupConfiguration ?? Configuration.RollupConfiguration.RollupDisabled,
                metricStoreConfiguration ?? (distinctCountConfiguration?.Dimensions.Any() == true ? Configuration.PublicationConfiguration.CacheServer : Configuration.PublicationConfiguration.MetricStore),
                distinctCountConfiguration ?? new DistinctCountConfiguration(),
                filteringConfiguration ?? Configuration.FilteringConfiguration.FilteringDisabled);
        }

        /// <summary>
        /// Creates a new preaggregate.
        /// </summary>
        /// <remarks>
        /// This is older legacy api for backward compatibility. Add new properties to CreatePreaggregationWithDefaultConfiguration instead.
        /// </remarks>
        /// <param name="name">The name.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="minMaxConfiguration">The minimum maximum configuration.</param>
        /// <param name="percentileConfiguration">The percentile configuration.</param>
        /// <param name="rollupConfiguration">The rollup configuration.</param>
        /// <param name="metricStoreConfiguration">The metric store configuration.</param>
        /// <param name="distinctCountConfiguration">The distinct count configuration.</param>
        /// <returns>The created preaggregate.</returns>
        [Obsolete("CreatePreaggregation is deprecated, please use CreatePreaggregationWithDefaults instead.")]
        public static Preaggregation CreatePreaggregation(
            string name,
            IEnumerable<string> dimensions,
            MinMaxConfiguration minMaxConfiguration,
            PercentileConfiguration percentileConfiguration,
            RollupConfiguration rollupConfiguration,
            PublicationConfiguration metricStoreConfiguration,
            DistinctCountConfiguration distinctCountConfiguration)
        {
            return CreatePreaggregationImpl(
                name,
                dimensions,
                minMaxConfiguration,
                percentileConfiguration,
                rollupConfiguration,
                metricStoreConfiguration,
                distinctCountConfiguration,
                Configuration.FilteringConfiguration.FilteringDisabled);
        }

        /// <summary>
        /// Adds the dimension to the preaggregate.
        /// </summary>
        /// <param name="dimensionToAdd">Name of the dimension to add.</param>
        public void AddDimension(string dimensionToAdd)
        {
            if (string.IsNullOrWhiteSpace(dimensionToAdd))
            {
                throw new ArgumentNullException(nameof(dimensionToAdd));
            }

            var index = 0;
            for (; index < this.dimensions.Count; ++index)
            {
                var comparison = string.Compare(this.dimensions[index], dimensionToAdd, StringComparison.OrdinalIgnoreCase);
                if (comparison == 0)
                {
                    throw new ConfigurationValidationException("Cannot add duplicate dimensions.", ValidationType.DuplicateDimension);
                }

                if (comparison > 0)
                {
                    break;
                }
            }

            if ((index + 1) == this.dimensions.Count)
            {
                this.dimensions.Add(dimensionToAdd);
            }
            else
            {
                this.dimensions.Insert(index, dimensionToAdd);
            }
        }

        /// <summary>
        /// Removes the dimension from the preaggregate.
        /// </summary>
        /// <param name="dimensionName">Name of the dimension.</param>
        public void RemoveDimension(string dimensionName)
        {
            this.dimensions.RemoveAll(x => string.Equals(x, dimensionName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Creates a new preaggregate.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="minMaxConfiguration">The minimum maximum configuration.</param>
        /// <param name="percentileConfiguration">The percentile configuration.</param>
        /// <param name="rollupConfiguration">The rollup configuration.</param>
        /// <param name="metricStoreConfiguration">The metric store configuration.</param>
        /// <param name="distinctCountConfiguration">The distinct count configuration.</param>
        /// <param name="filteringConfiguration">The filtering configuration.</param>
        /// <returns>The created preaggregate.</returns>
        private static Preaggregation CreatePreaggregationImpl(
            string name,
            IEnumerable<string> dimensions,
            MinMaxConfiguration minMaxConfiguration,
            PercentileConfiguration percentileConfiguration,
            RollupConfiguration rollupConfiguration,
            PublicationConfiguration metricStoreConfiguration,
            DistinctCountConfiguration distinctCountConfiguration,
            IFilteringConfiguration filteringConfiguration)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (dimensions == null)
            {
                throw new ArgumentNullException(nameof(dimensions));
            }

            var dimensionList = dimensions.ToList();
            dimensionList.Sort(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < (dimensionList.Count - 1); i++)
            {
                if (string.Equals(dimensionList[i], dimensionList[i + 1], StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Cannot create a preaggregate with duplicate dimensions.");
                }
            }

            if (minMaxConfiguration == null)
            {
                throw new ArgumentNullException(nameof(minMaxConfiguration));
            }

            if (percentileConfiguration == null)
            {
                throw new ArgumentNullException(nameof(percentileConfiguration));
            }

            if (rollupConfiguration == null)
            {
                throw new ArgumentNullException(nameof(rollupConfiguration));
            }

            if (metricStoreConfiguration == null)
            {
                throw new ArgumentNullException(nameof(metricStoreConfiguration));
            }

            if (distinctCountConfiguration == null)
            {
                throw new ArgumentNullException(nameof(distinctCountConfiguration));
            }

            if (filteringConfiguration == null)
            {
                throw new ArgumentNullException(nameof(filteringConfiguration));
            }

            return new Preaggregation(
                name,
                dimensionList,
                minMaxConfiguration,
                percentileConfiguration,
                rollupConfiguration,
                metricStoreConfiguration,
                distinctCountConfiguration,
                filteringConfiguration);
        }
    }
}
