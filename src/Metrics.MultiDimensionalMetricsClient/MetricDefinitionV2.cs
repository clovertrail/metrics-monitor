// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricDefinitionV2.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a metric definition.
    /// A metric definition uniquely identifies a metric name, set of dimensions emitted for this metric by a client.
    /// </summary>
    public sealed class MetricDefinitionV2 : IEquatable<MetricDefinitionV2>
    {
        private int hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricDefinitionV2"/> class.
        /// </summary>
        /// <param name="monitoringAccount">Metric monitoring account.</param>
        /// <param name="metricNamespace">Metric namespace name.</param>
        /// <param name="metricName">Metric name.</param>
        /// <param name="dimensionNames">Dimension names enumeration for the metric definition.</param>
        public MetricDefinitionV2(string monitoringAccount, string metricNamespace, string metricName, IEnumerable<string> dimensionNames)
        {
            this.MonitoringAccount = monitoringAccount;
            this.MetricNamespace = metricNamespace;
            this.MetricName = metricName;

            this.DimensionNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dimensionName in dimensionNames)
            {
                this.DimensionNames.Add(dimensionName);
            }

            this.hashCode = 524287;

            var temp = StringComparer.OrdinalIgnoreCase.GetHashCode(monitoringAccount);
            this.hashCode = (int)(((uint)this.hashCode << temp) | ((uint)this.hashCode >> (32 - temp))) ^ temp;
            this.hashCode = (int)(((uint)this.hashCode << temp) | ((uint)this.hashCode >> (32 - temp)));

            temp = StringComparer.OrdinalIgnoreCase.GetHashCode(metricNamespace);
            this.hashCode = (int)(((uint)this.hashCode << temp) | ((uint)this.hashCode >> (32 - temp))) ^ temp;
            this.hashCode = (int)(((uint)this.hashCode << temp) | ((uint)this.hashCode >> (32 - temp)));

            temp = StringComparer.OrdinalIgnoreCase.GetHashCode(metricName);
            this.hashCode = (int)(((uint)this.hashCode << temp) | ((uint)this.hashCode >> (32 - temp))) ^ temp;
            this.hashCode = (int)(((uint)this.hashCode << temp) | ((uint)this.hashCode >> (32 - temp)));

            foreach (var currentDimensionValue in this.DimensionNames)
            {
                if (string.IsNullOrWhiteSpace(currentDimensionValue))
                {
                    throw new ArgumentException("Dimension names cannot be null or empty strings.", nameof(dimensionNames));
                }

                temp = StringComparer.OrdinalIgnoreCase.GetHashCode(currentDimensionValue);
                this.hashCode = (int)(((uint)this.hashCode << temp) | ((uint)this.hashCode >> (32 - temp))) ^ temp;
                this.hashCode = (int)(((uint)this.hashCode << temp) | ((uint)this.hashCode >> (32 - temp)));
            }
        }

        /// <summary>
        /// The name of the monitoring account the metric belongs too.
        /// </summary>
        public string MonitoringAccount { get; private set; }

        /// <summary>
        /// The name of the namespace the metric belongs too.
        /// </summary>
        public string MetricNamespace { get; private set; }

        /// <summary>
        /// The metric name for current metric definition.
        /// </summary>
        public string MetricName { get; private set; }

        /// <summary>
        /// List of dimensions emitted for this metric definition instance.
        /// </summary>
        public SortedSet<string> DimensionNames { get; private set; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.hashCode;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as MetricDefinitionV2);
        }

        /// <inheritdoc/>
        public bool Equals(MetricDefinitionV2 other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.hashCode != other.hashCode ||
                this.DimensionNames.Count != other.DimensionNames.Count ||
                !this.MonitoringAccount.Equals(other.MonitoringAccount) ||
                !this.MonitoringAccount.Equals(other.MetricNamespace) ||
                !this.MonitoringAccount.Equals(other.MetricName))
            {
                return false;
            }

            return this.DimensionNames.SetEquals(other.DimensionNames);
        }
    }
}
