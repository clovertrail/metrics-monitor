// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PreAggregateConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JetBrains.Annotations;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents configuration for a pre-aggregate of a metric.
    /// </summary>
    public sealed class PreAggregateConfiguration : IEquatable<PreAggregateConfiguration>
    {
        private static readonly IEqualityComparer<ICollection<string>> DimensionsEqualityComparer =
            new CollectionEqualityComparer<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly IEqualityComparer<ICollection<string>> DistinctCountColumnsEqualityComparer =
            new CollectionEqualityComparer<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="PreAggregateConfiguration"/> class.
        /// </summary>
        /// <param name="displayName">The display name.</param>
        /// <param name="dimensions">The list of dimension names for the pre-aggregate.</param>
        /// <param name="minMaxMetricsEnabled">Flag indicating whether min/max sampling types are enabled.</param>
        /// <param name="percentileMetricsEnabled">Flag indicating whether percentile sampling type is enabled.</param>
        /// <param name="distinctCountColumns">The list of dimension names for the distinct count.</param>
        [JsonConstructor]
        public PreAggregateConfiguration(string displayName, IEnumerable<string> dimensions, bool minMaxMetricsEnabled, bool percentileMetricsEnabled, IEnumerable<string> distinctCountColumns = null)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentException("displayName is null or empty.");
            }

            if (dimensions == null)
            {
                throw new ArgumentNullException("dimensions");
            }

            this.DisplayName = displayName;
            this.Dimensions = dimensions.ToList();
            this.MinMaxMetricsEnabled = minMaxMetricsEnabled;
            this.PercentileMetricsEnabled = percentileMetricsEnabled;
            this.DistinctCountColumns = distinctCountColumns != null ? distinctCountColumns.ToList() : null;
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Gets the collections of dimensions which this pre-aggregate represents.
        /// </summary>
        public ICollection<string> Dimensions { get; private set; }

        /// <summary>
        /// Gets the collections of distinct count columns.
        /// </summary>
        public ICollection<string> DistinctCountColumns { get; private set; }

        /// <summary>
        /// Gets a value indicating whether min and max should be generated for the pre-aggregate.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool MinMaxMetricsEnabled { get; private set; }

        /// <summary>
        /// Gets a value indicating whether percentile should be generated for the pre-aggregate.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool PercentileMetricsEnabled { get; private set; }

        public static bool operator ==([CanBeNull] PreAggregateConfiguration left, [CanBeNull] PreAggregateConfiguration right)
        {
            return Equals(left, right);
        }

        public static bool operator !=([CanBeNull] PreAggregateConfiguration left, [CanBeNull] PreAggregateConfiguration right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(PreAggregateConfiguration other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(this.DisplayName, other.DisplayName, StringComparison.OrdinalIgnoreCase)
                   && DimensionsEqualityComparer.Equals(this.Dimensions, other.Dimensions)
                   && DistinctCountColumnsEqualityComparer.Equals(this.DistinctCountColumns, other.DistinctCountColumns)
                   && this.MinMaxMetricsEnabled == other.MinMaxMetricsEnabled
                   && this.PercentileMetricsEnabled == other.PercentileMetricsEnabled;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var castObj = obj as PreAggregateConfiguration;
            return castObj != null
                   && this.Equals(castObj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode Justification = "All properties have a private setter used only for Json Serialization, it is effectively readonly"
                var hashCode = this.DisplayName != null
                                   ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.DisplayName)
                                   : 0;
                hashCode = (hashCode * 397) ^ DimensionsEqualityComparer.GetHashCode(this.Dimensions);
                hashCode = (hashCode * 397)
                           ^ DistinctCountColumnsEqualityComparer.GetHashCode(this.DistinctCountColumns);
                hashCode = (hashCode * 397) ^ this.MinMaxMetricsEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^ this.PercentileMetricsEnabled.GetHashCode();

                // ReSharper restore NonReadonlyMemberInGetHashCode
                return hashCode;
            }
        }

        /// <summary>
        /// Compares two collections for equality by using the contained items' <see cref="IEquatable{T}" /> methods
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        private sealed class CollectionEqualityComparer<T> : IEqualityComparer<ICollection<T>>
        {
            private readonly IEqualityComparer<T> itemComparer;

            /// <summary>
            /// Initializes a new instance of the <see cref="CollectionEqualityComparer{T}"/> class.
            /// </summary>
            /// <param name="itemComparer">The item comparer.</param>
            internal CollectionEqualityComparer([NotNull] IEqualityComparer<T> itemComparer)
            {
                this.itemComparer = itemComparer;
            }

            /// <inheritdoc />
            bool IEqualityComparer<ICollection<T>>.Equals([CanBeNull] ICollection<T> x, [CanBeNull] ICollection<T> y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                {
                    return false;
                }

                if (x.Count != y.Count)
                {
                    return false;
                }

                int xNullCount;
                int yNullCount;
                IDictionary<T, int> xCountsDictionary = this.CreateCountDictionary(x, out xNullCount);
                IDictionary<T, int> yCountsDictionary = this.CreateCountDictionary(y, out yNullCount);

                if (xNullCount != yNullCount)
                {
                    return false;
                }

                foreach (var kvp in xCountsDictionary)
                {
                    int yValue;
                    if (!yCountsDictionary.TryGetValue(kvp.Key, out yValue))
                    {
                        return false;
                    }

                    if (kvp.Value != yValue)
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <inheritdoc />
            public int GetHashCode([CanBeNull] ICollection<T> obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                unchecked
                {
                    int hashCode = 0;
                    foreach (var item in obj)
                    {
                        hashCode += ReferenceEquals(item, null) ? -1 : this.itemComparer.GetHashCode(item);
                    }

                    return hashCode;
                }
            }

            [NotNull]
            private IDictionary<T, int> CreateCountDictionary([NotNull] ICollection<T> collection, out int nullCount)
            {
                // CODESYNC: Args passed to .GroupBy and .ToDictionary affect the construction of countDictionaryComparer
                Dictionary<T, int> countsDictionary = new Dictionary<T, int>(this.itemComparer);

                nullCount = 0;

                foreach (var item in collection)
                {
                    if (ReferenceEquals(item, null))
                    {
                        nullCount++;
                        continue;
                    }

                    if (!countsDictionary.ContainsKey(item))
                    {
                        countsDictionary[item] = 1;
                    }
                    else
                    {
                        countsDictionary[item]++;
                    }
                }

                return countsDictionary;
            }
        }
    }
}