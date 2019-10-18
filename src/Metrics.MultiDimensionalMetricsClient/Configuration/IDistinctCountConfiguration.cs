//-------------------------------------------------------------------------------------------------
// <copyright file="IDistinctCountConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Configures distinct count feature on this preaggregate.
    /// </summary>
    public interface IDistinctCountConfiguration
    {
        /// <summary>
        /// Gets the dimensions.
        /// </summary>
        IEnumerable<string> Dimensions { get; }

        /// <summary>
        /// Adds the dimension.
        /// </summary>
        /// <param name="dimension">The dimension.</param>
        void AddDimension(string dimension);

        /// <summary>
        /// Removes the dimension.
        /// </summary>
        /// <param name="dimension">The dimension.</param>
        void RemoveDimension(string dimension);
    }
}