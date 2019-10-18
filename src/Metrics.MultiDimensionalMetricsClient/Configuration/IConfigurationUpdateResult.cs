// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigurationUpdateResult.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// Interface to represent the configuration update result.
    /// </summary>
    public interface IConfigurationUpdateResult
    {
        /// <summary>
        /// Monitoring account on which configuration was updated.
        /// </summary>
        string MonitoringAccount { get; }

        /// <summary>
        /// True if configuration is updated successfully. False, otherwise.
        /// </summary>
        bool Success { get; set; }

        /// <summary>
        /// Exception details in case of failures.
        /// </summary>
        string Message { get; set; }
    }
}
