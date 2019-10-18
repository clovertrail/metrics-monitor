// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringAccount.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// The monitoring account configuration object used for JSON serialization and deserialization.
    /// </summary>
    public sealed class MonitoringAccount : IMonitoringAccount
    {
        /// <summary>
        /// List of permissions granted access to this account.
        /// </summary>
        private readonly IList<IPermissionV2> permissions;

        /// <summary>
        /// List of mirror monitoring accounts.
        /// </summary>
        private readonly IList<string> mirrorMonitoringAccountList;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoringAccount"/> class.
        /// </summary>
        /// <param name="name">The name of the account.</param>
        /// <param name="description">The description of the account.</param>
        /// <param name="permissionsV2">The permissions associated with the account.</param>
        public MonitoringAccount(
            string name,
            string description,
            IEnumerable<IPermissionV2> permissionsV2)
            : this(name, null, description, default(DateTime), permissionsV2, null, 1, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoringAccount" /> class.
        /// </summary>
        /// <param name="name">The name of the account.</param>
        /// <param name="displayName">The display name of the account.</param>
        /// <param name="description">The description of the account.</param>
        /// <param name="lastUpdatedTimeUtc">The last updated time.</param>
        /// <param name="permissionsV2">The permissions associated with the account.</param>
        /// <param name="lastUpdatedBy">The identity that last updated the account.</param>
        /// <param name="version">The version of the account.</param>
        /// <param name="mirrorMonitoringAccountList">The list of mirror monitoring accounts.</param>
        /// <param name="homeStampHostName">Name of the home stamp host for this account.</param>
        [JsonConstructor]
        internal MonitoringAccount(
            string name,
            string displayName,
            string description,
            DateTime lastUpdatedTimeUtc,
            IEnumerable<IPermissionV2> permissionsV2,
            string lastUpdatedBy,
            uint version,
            IEnumerable<string> mirrorMonitoringAccountList,
            string homeStampHostName)
        {
            if (permissionsV2 == null)
            {
                throw new ArgumentNullException(nameof(permissionsV2));
            }

            this.Name = name;
            this.DisplayName = displayName;
            this.Description = description;
            this.LastUpdatedTimeUtc = lastUpdatedTimeUtc;
            this.LastUpdatedBy = lastUpdatedBy;
            this.Version = version;
            this.permissions = permissionsV2.ToList();
            this.HomeStampHostName = homeStampHostName;

            this.mirrorMonitoringAccountList = mirrorMonitoringAccountList?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// The name of the monitoring account.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The display name of the monitoring account.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The description of the monitoring account.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The host name of the MDM stamp that currently owns this account.
        /// </summary>
        public string HomeStampHostName { get; }

        /// <summary>
        /// The list of entities that have access to this MDM account and their roles.
        /// </summary>
        [JsonProperty(PropertyName = "PermissionsV2")]
        public IEnumerable<IPermissionV2> Permissions
        {
            get { return this.permissions; }
        }

        /// <summary>
        /// The time the account was last updated.
        /// </summary>
        public DateTime LastUpdatedTimeUtc { get; }

        /// <summary>
        /// The identity that updated the account most recently.
        /// </summary>
        public string LastUpdatedBy { get; }

        /// <summary>
        /// The version of the monitoring account configuration.
        /// </summary>
        public uint Version { get; }

        /// <inheritdoc />
        public IEnumerable<string> MirrorMonitoringAccountList
        {
            get
            {
                return this.mirrorMonitoringAccountList;
            }
        }

        /// <summary>
        /// Gets or sets the value for the maximum age, as a <see cref="TimeSpan"/>, of metrics to be accepted.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal TimeSpan MaxMetricAge { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether to prefer to create new pre-aggregates on metrics store.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal bool? PreferNewPreaggregateOnMetricsStore { get; set; }

        /// <summary>
        /// Adds a permission to the account.
        /// </summary>
        /// <param name="permission">Permission to add.</param>
        public void AddPermission(IPermissionV2 permission)
        {
           this.permissions.Add(permission);
        }

        /// <summary>
        /// Remove permission from the account.
        /// </summary>
        /// <param name="permission">Permission to remove.</param>
        public void RemovePermission(IPermissionV2 permission)
        {
            this.permissions.Remove(permission);
        }

        /// <inheritdoc />
        public void AddMirrorMonitoringAccount(string monitoringAccountName)
        {
            this.mirrorMonitoringAccountList.Add(monitoringAccountName);
        }

        /// <inheritdoc />
        public void RemoveMirrorMonitoringAccount(string monitoringAccountName)
        {
            this.mirrorMonitoringAccountList.Remove(monitoringAccountName);
        }
    }
}
