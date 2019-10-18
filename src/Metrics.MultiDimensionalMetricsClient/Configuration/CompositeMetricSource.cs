//-------------------------------------------------------------------------------------------------
// <copyright file="CompositeMetricSource.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;

    /// <summary>
    /// Identity and display name of the metric that will be used in the composite.
    /// </summary>
    public class CompositeMetricSource
    {
        private string displayName;
        private string monitoringAccount;
        private string metricNamespace;
        private string metric;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeMetricSource"/> class.
        /// </summary>
        /// <param name="displayName">The display name.</param>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metric">The metric.</param>
        public CompositeMetricSource(string displayName, string monitoringAccount, string metricNamespace, string metric)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(monitoringAccount))
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrWhiteSpace(metricNamespace))
            {
                throw new ArgumentNullException(nameof(metricNamespace));
            }

            if (string.IsNullOrWhiteSpace(metric))
            {
                throw new ArgumentNullException(nameof(metric));
            }

            this.displayName = displayName;
            this.monitoringAccount = monitoringAccount;
            this.metricNamespace = metricNamespace;
            this.metric = metric;
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return this.displayName;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.displayName = value;
            }
        }

        /// <summary>
        /// Gets or sets the monitoring account.
        /// </summary>
        public string MonitoringAccount
        {
            get
            {
                return this.monitoringAccount;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.monitoringAccount = value;
            }
        }

        /// <summary>
        /// Gets or sets the metric namespace.
        /// </summary>
        public string MetricNamespace
        {
            get
            {
                return this.metricNamespace;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.metricNamespace = value;
            }
        }

        /// <summary>
        /// Gets or sets the metric.
        /// </summary>
        public string Metric
        {
            get
            {
                return this.metric;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.metric = value;
            }
        }
    }
}