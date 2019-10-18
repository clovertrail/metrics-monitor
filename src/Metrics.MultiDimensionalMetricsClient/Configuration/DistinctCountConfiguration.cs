//-------------------------------------------------------------------------------------------------
// <copyright file="DistinctCountConfiguration.cs" company="Microsoft Corporation">
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
    /// Configures distinct count feature on this preaggregate.
    /// </summary>
    public sealed class DistinctCountConfiguration : IDistinctCountConfiguration
    {
        private readonly List<string> dimensions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistinctCountConfiguration"/> class.
        /// </summary>
        public DistinctCountConfiguration()
        {
            this.dimensions = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistinctCountConfiguration"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        [JsonConstructor]
        internal DistinctCountConfiguration(IEnumerable<string> dimensions)
        {
            this.dimensions = dimensions?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Gets the dimensions.
        /// </summary>
        public IEnumerable<string> Dimensions
        {
            get { return this.dimensions; }
        }

        /// <summary>
        /// Adds the dimension.
        /// </summary>
        /// <param name="dimensionToAdd">The dimension to add.</param>
        public void AddDimension(string dimensionToAdd)
        {
            if (string.IsNullOrWhiteSpace(dimensionToAdd))
            {
                throw new ArgumentNullException(nameof(dimensionToAdd));
            }

            if (this.dimensions.Count == 0)
            {
                this.dimensions.Add(dimensionToAdd);
                return;
            }

            for (var i = 0; i < this.dimensions.Count; ++i)
            {
                var comparison = string.Compare(this.dimensions[i], dimensionToAdd, StringComparison.OrdinalIgnoreCase);
                if (comparison == 0)
                {
                    throw new ConfigurationValidationException("Cannot add duplicate dimensions.", ValidationType.DuplicateDimension);
                }

                if (comparison > 0)
                {
                    this.dimensions.Insert(i, dimensionToAdd);
                    return;
                }

                if ((i + 1) == this.dimensions.Count)
                {
                    this.dimensions.Add(dimensionToAdd);
                    return;
                }
            }
        }

        /// <summary>
        /// Removes the dimension.
        /// </summary>
        /// <param name="dimension">The dimension.</param>
        public void RemoveDimension(string dimension)
        {
            this.dimensions.RemoveAll(x => string.Equals(x, dimension, StringComparison.OrdinalIgnoreCase));
        }
    }
}
