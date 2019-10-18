// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogSketches.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a List of Distinct Count dimension name and HyperLogLogSketch associated with it.
    /// </summary>
    public class HyperLogLogSketches : List<KeyValuePair<string, HyperLogLogSketch>>, IReadOnlyHyperLogLogSketches
    {
        /// <summary>
        /// Constucts an instance of class HyperLogLogSketches
        /// </summary>
        public uint HyperLogLogSketchesCount
        {
            get
            {
                return (uint)this.Count;
            }
        }

        /// <summary>
        /// Implements the interface IReadOnlyHyperLogLogSketches
        /// </summary>
        IEnumerable<KeyValuePair<string, HyperLogLogSketch>> IReadOnlyHyperLogLogSketches.HyperLogLogSketches
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Sets the register value for given key.
        /// </summary>
        /// <param name="key">Index value.</param>
        /// <returns>A <see cref="HyperLogLogSketch"/> instance at the given index.</returns>
        public HyperLogLogSketch this[string key]
        {
            get
            {
                var index = this.Find(key);
                if (index < 0)
                {
                    throw new KeyNotFoundException(string.Format("{0} not found in sketches list", key));
                }

                return this[index].Value;
            }

            set
            {
                this.Add(key, value);
            }
        }

        /// <summary>
        /// Checks if given distinct count dimension exists.
        /// </summary>
        /// <param name="distinctCountDimensionName">Distinct count dimension name.</param>
        /// <returns>True if exists else false.</returns>
        public bool ContainsKey(string distinctCountDimensionName)
        {
            return this.Find(distinctCountDimensionName) >= 0;
        }

        /// <summary>
        /// Adds a key/value pair to <see cref="HyperLogLogSketches"/> by using the specified function, if the key does not already exist.
        /// </summary>
        /// <param name="key">Key of the element.</param>
        /// <param name="valueFactory">The function used to generate <see cref="HyperLogLogSketch"/>.</param>
        /// <returns><see cref="HyperLogLogSketch"/> for specified key. This will be either the existing value or the new value returned by valueFactory (if specified key is not found).</returns>
        public HyperLogLogSketch GetOrAdd(string key, Func<HyperLogLogSketch> valueFactory)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            var index = this.Find(key);
            HyperLogLogSketch hyperLogLogSketch;

            if (index >= 0)
            {
                hyperLogLogSketch = this[index].Value;
            }
            else
            {
                hyperLogLogSketch = valueFactory();
                this.Add(new KeyValuePair<string, HyperLogLogSketch>(key, hyperLogLogSketch));
            }

            return hyperLogLogSketch;
        }

        /// <summary>
        /// Adds the given distinct count dimension and sketch to the list.
        /// </summary>
        /// <param name="distinctCountDimensionName">Distinct count dimension name.</param>
        /// <param name="sketch">Sketch data.</param>
        public void Add(string distinctCountDimensionName, HyperLogLogSketch sketch)
        {
            var index = this.Find(distinctCountDimensionName);

            if (index >= 0)
            {
                this[index] = new KeyValuePair<string, HyperLogLogSketch>(distinctCountDimensionName, sketch);
            }
            else
            {
                this.Add(new KeyValuePair<string, HyperLogLogSketch>(distinctCountDimensionName, sketch));
            }
        }

        private int Find(string key)
        {
            int index = -1;

            for (int i = 0; i < this.Count; i++)
            {
                if (this[i].Key.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            return index;
        }
    }
}
