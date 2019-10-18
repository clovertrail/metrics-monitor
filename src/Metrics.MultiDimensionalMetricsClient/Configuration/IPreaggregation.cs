//-------------------------------------------------------------------------------------------------
// <copyright file="IPreaggregation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// A grouping of dimensions used to aggregate metric data.
    /// </summary>
    public interface IPreaggregation
    {
        /// <summary>
        /// Gets or sets the name of the preaggregate.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the dimensions of the preaggregate in sorted order..
        /// </summary>
        IEnumerable<string> Dimensions { get; }

        /// <summary>
        /// The min/max sampling type configuration.
        /// </summary>
        IMinMaxConfiguration MinMaxConfiguration { get; set; }

        /// <summary>
        /// The percentile sampling type configuration.
        /// </summary>
        IPercentileConfiguration PercentileConfiguration { get; set; }

        /// <summary>
        /// The data rollup configuration.
        /// </summary>
        IRollupConfiguration RollupConfiguration { get; set; }

        /// <summary>
        /// The metric data store configuration.
        /// </summary>
        IPublicationConfiguration PublicationConfiguration { get; set; }

        /// <summary>
        /// The distinct count sampling type configuration.
        /// </summary>
        IDistinctCountConfiguration DistinctCountConfiguration { get; set; }

        /// <summary>
        /// The filtering configuration.
        /// </summary>
        IFilteringConfiguration FilteringConfiguration { get; set; }

        /// <summary>
        /// Adds the dimension to the preaggregate.
        /// </summary>
        /// <param name="dimensionName">Name of the dimension.</param>
        void AddDimension(string dimensionName);

        /// <summary>
        /// Removes the dimension from the preaggregate.
        /// </summary>
        /// <param name="dimensionName">Name of the dimension.</param>
        void RemoveDimension(string dimensionName);
    }
}
