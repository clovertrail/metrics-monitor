// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Role.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// Determines what level of access an entity has to an MDM entity.
    /// </summary>
    public enum Role
    {
        /// <summary>
        /// For certificates only - can read metric data but cannot make any changes.
        /// </summary>
        ReadOnly,

        /// <summary>
        /// This role has the ability to modify and create dashboards within an account.
        /// </summary>
        DashboardEditor,

        /// <summary>
        /// This role has the ability to modify metric, monitor or health configuration within an account.
        /// </summary>
        ConfigurationEditor,

        /// <summary>
        /// For certificates only - can publish metrics but cannot make other changes.
        /// </summary>
        MetricPublisher,

        /// <summary>
        /// Full access to modify configuration, account settings and dashboards.
        /// </summary>
        Administrator,

        /// <summary>
        /// This role has the ability to modify and create monitors within an account.
        /// </summary>
        MonitorEditor,

        /// <summary>
        /// This role has the ability to modify and create monitors/metrics within an account.
        /// </summary>
        MetricAndMonitorEditor
    }
}
