// <copyright file="PreaggregateDimensionFilterValues.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

// ReSharper disable once CheckNamespace
namespace Microsoft.Cloud.Metrics.Client.PreaggregateFiltersManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents filter values for a single preaggregate dimension name.
    /// </summary>
    [JsonObject]
    internal sealed class PreaggregateDimensionFilterValues
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreaggregateDimensionFilterValues"/> class.
        /// </summary>
        /// <param name="filterDimensionName">Name of the filter dimension.</param>
        /// <param name="filterValues">The filter values.</param>
        [JsonConstructor]
        public PreaggregateDimensionFilterValues(string filterDimensionName, IReadOnlyList<string> filterValues)
        {
            if (string.IsNullOrEmpty(filterDimensionName))
            {
                throw new ArgumentNullException(nameof(filterDimensionName));
            }

            if (filterValues == null)
            {
                throw new ArgumentNullException(nameof(filterValues));
            }

            foreach (var value in filterValues)
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException($"{nameof(filterValues)} cannot have empty of null values");
                }
            }

            this.FilterDimensionName = filterDimensionName;
            this.FilterValues = new HashSet<string>(filterValues, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the name of the filter dimension.
        /// </summary>
        public string FilterDimensionName { get; }

        /// <summary>
        /// Gets the filter values.
        /// </summary>
        public ISet<string> FilterValues { get; }
    }
}
