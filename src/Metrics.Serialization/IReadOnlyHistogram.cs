// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IReadOnlyHistogram.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <author email="selavrin">
//     Sergii Lavrinenko
// </author>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System.Collections.Generic;

    /// <summary>
    /// Read-only interface for objects representing histograms.
    /// </summary>
    public interface IReadOnlyHistogram
    {
        /// <summary>
        /// Gets the number of samples in the histogram.
        /// </summary>
        int SamplesCount { get; }

        /// <summary>
        /// Gets the list of histogram samples: ordered pairs of value-count.
        /// </summary>
        IEnumerable<KeyValuePair<ulong, uint>> Samples
        {
            get;
        }

        /// <summary>
        /// Calculates percentile from the histogram.
        /// </summary>
        /// <param name="percent">Percent value for which to calculate percentile.</param>
        /// <returns>Percentile value.</returns>
        float GetPercentile(float percent);
    }
}
