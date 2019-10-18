﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserPermissionV2.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// User with access to MDM.
    /// </summary>
    public sealed class UserPermissionV2 : IPermissionV2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserPermissionV2"/> class.
        /// Create a new user for MDM access.
        /// </summary>
        /// <param name="identity">The alias of the user.</param>
        /// <param name="roleConfiguration">The role assigned to this user.</param>
        [JsonConstructor]
        public UserPermissionV2(string identity, RoleConfiguration roleConfiguration)
        {
            this.Identity = identity;
            this.Description = null;
            this.RoleConfiguration = roleConfiguration;
        }

        /// <summary>
        /// The identity to grant permission.
        /// </summary>
        public string Identity { get; }

        /// <inheritdoc />
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Description { get; }

        /// <summary>
        /// The level of access to be granted to this identity.
        /// </summary>
        public RoleConfiguration RoleConfiguration { get; set; }
    }
}
