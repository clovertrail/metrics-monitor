// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DimensionFilter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// A filter to include only specific dimension values, if any.
    /// </summary>
    public sealed class DimensionFilter
    {
        /// <summary>
        /// The dimension name.
        /// </summary>
        private readonly string dimensionName;

        /// <summary>
        /// The dimension values.
        /// </summary>
        private readonly string[] dimensionValues;

        /// <summary>
        /// Flag to indicate if this is exclude filter.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed. Suppression is OK here.")]
        private readonly bool isExcludeFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DimensionFilter" /> class.
        /// </summary>
        /// <param name="dimensionName">The dimension name.</param>
        /// <param name="dimensionValues">The dimension values.</param>
        /// <param name="isExcludeFilter">If set to <c>true</c> [is exclude filter].</param>
        /// <remarks>
        /// By default, this is an include filter.
        /// </remarks>
        [JsonConstructor]
        public DimensionFilter(string dimensionName, IEnumerable<string> dimensionValues, bool isExcludeFilter)
        {
            if (string.IsNullOrWhiteSpace(dimensionName))
            {
                throw new ArgumentException("dimensionName is null or empty");
            }

            this.dimensionName = dimensionName;

            this.dimensionValues = dimensionValues != null ? dimensionValues.ToArray() : null;

            this.isExcludeFilter = isExcludeFilter;
        }

        /// <summary>
        /// Gets the dimension name.
        /// </summary>
        public string DimensionName
        {
            get
            {
                return this.dimensionName;
            }
        }

        /// <summary>
        /// The dimension values.
        /// </summary>
        public IReadOnlyList<string> DimensionValues
        {
            get
            {
                return this.dimensionValues;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is an exclude dimension filter.
        /// </summary>
        public bool IsExcludeFilter
        {
            get
            {
                return this.isExcludeFilter;
            }
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="string"/> to <see cref="DimensionFilter"/>.
        /// </summary>
        /// <param name="dimensionName">The dimension name.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator DimensionFilter(string dimensionName)
        {
            return CreateIncludeFilter(dimensionName);
        }

        /// <summary>
        /// Creates an include dimension filter.
        /// </summary>
        /// <param name="dimensionName">The dimension name.</param>
        /// <param name="dimensionValues">The dimension values.</param>
        /// <returns>An include dimension filter.</returns>
        public static DimensionFilter CreateIncludeFilter(string dimensionName, params string[] dimensionValues)
        {
            return CreateIncludeFilter(dimensionName, dimensionValues.AsEnumerable());
        }

        /// <summary>
        /// Creates an include dimension filter.
        /// </summary>
        /// <param name="dimensionName">The dimension name.</param>
        /// <param name="dimensionValues">The dimension values.</param>
        /// <returns>An include dimension filter.</returns>
        public static DimensionFilter CreateIncludeFilter(string dimensionName, IEnumerable<string> dimensionValues)
        {
            return new DimensionFilter(dimensionName, dimensionValues, isExcludeFilter: false);
        }

        /// <summary>
        /// Creates an exclude dimension filter.
        /// </summary>
        /// <param name="dimensionName">The dimension name.</param>
        /// <param name="dimensionValues">The dimension values.</param>
        /// <returns>An exclude dimension filter.</returns>
        public static DimensionFilter CreateExcludeFilter(string dimensionName, params string[] dimensionValues)
        {
            return CreateExcludeFilter(dimensionName, dimensionValues.AsEnumerable());
        }

        /// <summary>
        /// Creates an exclude dimension filter.
        /// </summary>
        /// <param name="dimensionName">The dimension name.</param>
        /// <param name="dimensionValues">The dimension values.</param>
        /// <returns>An exclude dimension filter.</returns>
        public static DimensionFilter CreateExcludeFilter(string dimensionName, IEnumerable<string> dimensionValues)
        {
            return new DimensionFilter(dimensionName, dimensionValues, isExcludeFilter: true);
        }
    }
}