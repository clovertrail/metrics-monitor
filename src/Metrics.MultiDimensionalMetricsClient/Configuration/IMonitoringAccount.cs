// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMonitoringAccount.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This object represents one MDM Monitoring Account, which is used to grant permission
    /// to users, certificates, and groups.
    /// </summary>
    public interface IMonitoringAccount
    {
        /// <summary>
        /// The name of the monitoring account.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The display name of the monitoring account.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// The description of the monitoring account.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// The host name of the MDM stamp that currently owns this account.
        /// </summary>
        string HomeStampHostName { get; }

        /// <summary>
        /// The list of entities that have access to this MDM account and their roles.
        /// </summary>
        IEnumerable<IPermissionV2> Permissions { get; }

        /// <summary>
        /// The time the account was last updated.
        /// </summary>
        DateTime LastUpdatedTimeUtc { get; }

        /// <summary>
        /// The identity that updated the account most recently.
        /// </summary>
        string LastUpdatedBy { get; }

        /// <summary>
        /// The version of the monitoring account configuration.
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Gets the list of mirror monitoring accounts.
        /// </summary>
        IEnumerable<string> MirrorMonitoringAccountList { get; }

        /// <summary>
        /// Adds a permission to the account.
        /// </summary>
        /// <param name="permission">Permission to add.</param>
        void AddPermission(IPermissionV2 permission);

        /// <summary>
        /// Remove permission from the account.
        /// </summary>
        /// <param name="permission">Permission to remove.</param>
        void RemovePermission(IPermissionV2 permission);

        /// <summary>
        /// Adds a monitoring account to the mirror monitoring account list.
        /// </summary>
        /// <param name="monitoringAccountName">Name of the monitoring account.</param>
        void AddMirrorMonitoringAccount(string monitoringAccountName);

        /// <summary>
        /// Removes a monitoring account from the mirror monitoring account list.
        /// </summary>
        /// <param name="monitoringAccountName">Name of the monitoring account.</param>
        void RemoveMirrorMonitoringAccount(string monitoringAccountName);
    }
}
