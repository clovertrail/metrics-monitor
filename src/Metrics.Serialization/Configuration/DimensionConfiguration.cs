// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DimensionConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.Configuration
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the configuration of a dimension.
    /// </summary>
    public sealed class DimensionConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DimensionConfiguration"/> class.
        /// </summary>
        /// <param name="id">The dimension name.</param>
        public DimensionConfiguration(string id)
            : this(id, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DimensionConfiguration"/> class.
        /// </summary>
        /// <param name="id">The dimension name.</param>
        /// <param name="dimensionValuesToIgnore">The dimension values to ignore.</param>
        [JsonConstructor]
        public DimensionConfiguration(string id, IList<string> dimensionValuesToIgnore)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id is null or empty.");
            }

            this.Id = id;
            this.DimensionValuesToIgnore = dimensionValuesToIgnore;
        }

        /// <summary>
        /// Gets the identifier of this instance.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets or sets the dimension values to ignore.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<string> DimensionValuesToIgnore { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long Identifier { get; set; }

        /// <summary>
        /// Validates the current instance and throws a <see cref="ArgumentException"/> if the instance is invalid.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(this.Id))
            {
                throw new ArgumentException("Property 'Id' is null or empty.");
            }
        }
    }
}