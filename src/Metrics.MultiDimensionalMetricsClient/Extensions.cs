//-------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Online.Metrics.Serialization.Configuration;

    /// <summary>
    /// The extension class.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the index of the <paramref name="dimensionName"/> in dimension combination list.
        /// </summary>
        /// <param name="definitions">The time series definitions.</param>
        /// <param name="dimensionName">Name of the dimension.</param>
        /// <returns>The index of the <paramref name="dimensionName"/> in dimension combination list, or -1 if not found.</returns>
        public static int GetIndexInDimensionCombination(this IReadOnlyList<TimeSeriesDefinition<MetricIdentifier>> definitions, string dimensionName)
        {
            if (definitions == null || definitions.Count == 0)
            {
                throw new ArgumentException("definitions is null or empty.");
            }

            if (string.IsNullOrWhiteSpace(dimensionName))
            {
                throw new ArgumentException("dimensionName is null or empty.");
            }

            var definition = definitions[0];
            if (definition.DimensionCombination == null)
            {
                return -1;
            }

            for (int i = 0; i < definition.DimensionCombination.Count; ++i)
            {
                if (dimensionName.Equals(definition.DimensionCombination[i].Key, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
