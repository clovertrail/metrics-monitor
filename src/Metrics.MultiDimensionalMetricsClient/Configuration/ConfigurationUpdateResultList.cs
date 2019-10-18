// <copyright file="ConfigurationUpdateResultList.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Stores list of configuration update result for a configuration.
    /// </summary>
    public sealed class ConfigurationUpdateResultList
    {
        /// <summary>
        /// Gets or sets the monitoring account.
        /// </summary>
        public string MonitoringAccount { get; set; }

        /// <summary>
        /// Gets or sets the metric namespace.
        /// </summary>
        public string MetricNamespace { get; set; }

        /// <summary>
        /// Gets or sets the name of the metric.
        /// </summary>
        public string MetricName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ConfigurationUpdateResultList"/> is success.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the exception message if the update is not successful.
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// Gets or sets the configuration update results.
        /// </summary>
        public IReadOnlyList<IConfigurationUpdateResult> ConfigurationUpdateResults { get; set; }
    }
}
