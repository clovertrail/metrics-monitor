// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMonitoringAccountAcls.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a list of ACLs associated with a monitoring account.
    /// </summary>
    /// <remarks>
    /// This does not include AP PKI as the cert is automatically generated and the ACL itself is not needed client side.
    /// </remarks>
    internal interface IMonitoringAccountAcls
    {
        /// <summary>
        /// Gets the thumbprints.
        /// </summary>
        List<string> Thumbprints { get; }

        /// <summary>
        /// Gets the dSMS acls.
        /// </summary>
        List<string> DsmsAcls { get; }

        /// <summary>
        /// Gets the KeyVault acls.
        /// </summary>
        List<string> KeyVaultAcls { get; }
    }
}
