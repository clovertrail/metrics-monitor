// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SecurityGroupV2.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Security group with access to MDM.
    /// </summary>
    public sealed class SecurityGroupV2 : IPermissionV2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityGroupV2"/> class.
        /// Create a new security group for MDM access.
        /// </summary>
        /// <param name="identity">The name of the security group.</param>
        /// <param name="roleConfiguration">The role granted to this security group.</param>
        [JsonConstructor]
        public SecurityGroupV2(string identity, RoleConfiguration roleConfiguration)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException(nameof(identity));
            }

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
