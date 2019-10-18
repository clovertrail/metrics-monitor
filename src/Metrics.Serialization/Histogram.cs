// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Histogram.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <author email="selavrin">
//     Sergii Lavrinenko
// </author>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Metrics.Services.Common;

    /// <summary>
    /// Represents a histogram.
    /// </summary>
    public sealed class Histogram : IReadOnlyHistogram, IPoolTrackable
    {
        private readonly List<KeyValuePair<ulong, uint>> histogram = new List<KeyValuePair<ulong, uint>>();
        private uint count;

        /// <inheritdoc />
        PoolObjectTrackingInfo IPoolTrackable.PoolObjectTrackingInfo { get; set; }

        /// <summary>
        /// Gets the number of samples in the histogram.
        /// </summary>
        public int SamplesCount => this.histogram.Count;

        /// <summary>
        /// Gets the list of histogram samples: ordered pairs of value-count.
        /// </summary>
        public IEnumerable<KeyValuePair<ulong, uint>> Samples => this.histogram;

        /// <summary>
        /// Reinitializes histogram object with new data.
        /// </summary>
        /// <param name="histogramData">Unordered pairs of value-count from which histogram will be constructed.</param>
        public void Initialize(IEnumerable<KeyValuePair<ulong, uint>> histogramData)
        {
            this.histogram.Clear();
            this.histogram.AddRange(histogramData);
            this.histogram.Sort((i1, i2) => (int)i1.Key - (int)i2.Key);
            this.count = (uint)this.histogram.Sum(h => h.Value);
        }

        /// <summary>
        /// Calculates percentile from the histogram.
        /// </summary>
        /// <param name="percent">Percent value for which to calculate percentile.</param>
        /// <returns>Percentile value.</returns>
        public float GetPercentile(float percent)
        {
            if (this.count == 0)
            {
                return 0;
            }

            if (percent < 0 || percent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percent), "Percent should be within [0;100] range.");
            }

            // Find index of the first value, whose last entry index is higher than index of the percentile for given percent
            float percentileIndex = percent * this.count / 100;
            uint currentIndex = 0;
            int index = -1;
            for (int i = 0; i < this.histogram.Count; ++i)
            {
                currentIndex += this.histogram[i].Value;
                if (percentileIndex <= currentIndex)
                {
                    index = i;
                    break;
                }
            }

            // Calculate percentile value based on found index
            if (index == 0)
            {
                return this.histogram[index].Key;
            }

            // When percentile index lies between two row values in the original sorted array, use weighted average to calculate percentile value (same approach is used in PerfCollector)
            var coefficient = percentileIndex - currentIndex + this.histogram[index].Value;
            return coefficient < 1 ?
                       (this.histogram[index - 1].Key * (1 - coefficient)) + (this.histogram[index].Key * coefficient) : this.histogram[index].Key;
        }
    }
}