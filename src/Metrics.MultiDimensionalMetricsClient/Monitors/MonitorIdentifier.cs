// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MonitorIdentifier.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Monitors
{
    using System;

    using Microsoft.Online.Metrics.Serialization.Configuration;

    using Newtonsoft.Json;

    /// <summary>
    /// A class representing a monitor identifier.
    /// </summary>
    public struct MonitorIdentifier : IEquatable<MonitorIdentifier>
    {
        /// <summary>
        /// The metric identifier.
        /// </summary>
        private readonly MetricIdentifier metricIdentifier;

        /// <summary>
        /// The monitor ID as in the monitor configuration.
        /// </summary>
        private readonly string monitorId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorIdentifier"/> struct.
        /// </summary>
        /// <param name="metricIdentifier">The metric identifier.</param>
        /// <param name="monitorId">The monitor identifier.</param>
        [JsonConstructor]
        public MonitorIdentifier(MetricIdentifier metricIdentifier, string monitorId)
        {
            if (monitorId == null)
            {
                throw new ArgumentNullException("monitorId");
            }

            this.metricIdentifier = metricIdentifier;
            this.monitorId = monitorId;
        }

        /// <summary>
        /// Gets the metric identifier.
        /// </summary>
        public MetricIdentifier MetricIdentifier
        {
            get
            {
                return this.metricIdentifier;
            }
        }

        /// <summary>
        /// Gets the monitor ID as in the monitor configuration.
        /// </summary>
        public string MonitorId
        {
            get
            {
                return this.monitorId;
            }
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(MonitorIdentifier other)
        {
            return this.metricIdentifier.Equals(other.metricIdentifier)
                   && string.Equals(this.monitorId, other.monitorId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified <paramref name="obj"/> is equal to this instance.
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

            return obj is MonitorIdentifier && this.Equals((MonitorIdentifier)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (this.metricIdentifier.GetHashCode() * 397)
                       ^ (this.monitorId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.monitorId) : 0);
            }
        }

        /// <summary>
        /// Validates this instance.
        /// </summary>
        internal void Validate()
        {
            // ReSharper disable once ImpureMethodCallOnReadonlyValueField
            this.metricIdentifier.Validate();

            if (string.IsNullOrWhiteSpace(this.monitorId))
            {
                throw new ArgumentException("monitorId is null or empty.");
            }
        }
    }
}