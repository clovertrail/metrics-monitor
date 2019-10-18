// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPermissionV2.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using Newtonsoft.Json;

    /// <summary>
    /// This object represents one entity and their level of access to an MDM Account.
    /// </summary>
    public interface IPermissionV2
    {
        /// <summary>
        /// The identity to grant permission.
        /// </summary>
        string Identity { get; }

        /// <summary>
        /// Gets the description of the permission.
        /// </summary>
        /// <remarks>Description is always null for <see cref="SecurityGroupV2"/> and <see cref="UserPermissionV2"/> class.</remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        string Description { get; }

        /// <summary>
        /// The level of access to be granted to this identity.
        /// </summary>
        RoleConfiguration RoleConfiguration { get; set; }
    }
}
