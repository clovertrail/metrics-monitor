// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IReadOnlyHyperLogLogSketches.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System.Collections.Generic;

    /// <summary>
    /// Read-only interface for objects representing list of hyperloglog sketches.
    /// </summary>
    public interface IReadOnlyHyperLogLogSketches
    {
        /// <summary>
        /// Gets the total sketches count.
        /// </summary>
        uint HyperLogLogSketchesCount { get; }

        /// <summary>
        /// Gets the list of sketches: ordered pairs of distinct count dimension name and HyperLogLogSketch.
        /// </summary>
        IEnumerable<KeyValuePair<string, HyperLogLogSketch>> HyperLogLogSketches
        {
            get;
        }
    }
}