// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationUpdateResult.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using Newtonsoft.Json;

    /// <summary>
    /// A class to represent the configuration update result.
    /// </summary>
    public sealed class ConfigurationUpdateResult : IConfigurationUpdateResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationUpdateResult"/> class.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account name.</param>
        /// <param name="success">Success result of operation.</param>
        /// <param name="message">Exception details if any.</param>
        [JsonConstructor]
        public ConfigurationUpdateResult(
            string monitoringAccount,
            bool success,
            string message)
        {
            this.MonitoringAccount = monitoringAccount;
            this.Success = success;
            this.Message = message;
        }

        /// <summary>
        /// Monitoring account on which configuration was updated.
        /// </summary>
        public string MonitoringAccount { get; }

        /// <summary>
        /// True if configuration is updated successfully. False, otherwise.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Exception details in case of failures.
        /// </summary>
        public string Message { get; set; }
    }
}
