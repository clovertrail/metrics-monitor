// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricFilterDetails.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.Configuration
{
    /// <summary>
    /// The details of the filter in order to filter the hints.
    /// </summary>
    public sealed class MetricFilterDetails
    {
        /// <summary>
        /// The prefix to be used before a sampling type in the <see cref="Expression"/>.
        /// </summary>
        public const string Prefix = "raw";

        /// <summary>
        /// The expression to be applied on each and every minutely data within the specified time interval.
        /// Example: <see cref="Prefix"/>.Sum &gt; 100 && <see cref="Prefix"/>.Count &lt; 2
        /// </summary>
        /// <remarks>
        /// The filter expression in <see cref="MetricFilterDetails.Expression"/> is applied on minutely values
        /// and the value of the dimension is returned even if the one of the minutely data returns true for the expression provided.
        /// </remarks>
        public string Expression { get; set; }

        /// <summary>
        /// Validates the contents of this instance.
        /// </summary>
        /// <returns>True if the filter is valid otherwise false.</returns>
        public bool IsValid()
        {
            return this.Expression != null && !string.IsNullOrEmpty(this.Expression.Trim());
        }
    }
}