// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RoleConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// The configuration for the MDM role which grants certain premissions to entities using the system.
    /// </summary>
    public sealed class RoleConfiguration
    {
        /// <summary>
        /// This role is meant for a certificate only since users all have read access.
        /// </summary>
        public static readonly RoleConfiguration ReadOnly = new RoleConfiguration("ReadOnly");

        /// <summary>
        /// This role is meant for a certificate primarily but does not need to be restricted to such.
        /// Allows a certificate to read or publish data only but not be used for account settings modification.
        /// </summary>
        public static readonly RoleConfiguration MetricPublisher = new RoleConfiguration("MetricPublisher");

        /// <summary>
        /// This role has the ability to modify and create dashboards within an account.
        /// </summary>
        public static readonly RoleConfiguration DashboardEditor = new RoleConfiguration("DashboardEditor");

        /// <summary>
        /// This role has the ability to modify and create monitors within an account.
        /// </summary>
        public static readonly RoleConfiguration MonitorEditor = new RoleConfiguration("MonitorEditor");

        /// <summary>
        /// This role has the ability to modify and create monitors/metrics within an account.
        /// </summary>
        public static readonly RoleConfiguration MetricAndMonitorEditor = new RoleConfiguration("MetricAndMonitorEditor");

        /// <summary>
        /// This role has the ability to modify metric, monitor or health configuration within an account.
        /// </summary>
        public static readonly RoleConfiguration ConfigurationEditor = new RoleConfiguration("ConfigurationEditor");

        /// <summary>
        /// Full access to modify configuration, account settings and dashboards.
        /// </summary>
        public static readonly RoleConfiguration Administrator = new RoleConfiguration("Administrator");

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleConfiguration"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public RoleConfiguration(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the role.
        /// </summary>
        public string Name { get; }
    }
}
