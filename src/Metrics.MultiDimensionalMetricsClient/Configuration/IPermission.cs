// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPermission.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;

    /// <summary>
    /// This object represents one entity and their level of access to an MDM Account.
    /// </summary>
    [Obsolete]
    public interface IPermission
    {
        /// <summary>
        /// The identity to grant permission.
        /// </summary>
        string Identity { get; }

        /// <summary>
        /// The level of access to be granted to this identity.
        /// </summary>
        Role Role { get; set; }
    }
}
