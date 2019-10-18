// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DsmsAcl.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// DSMS ACL with access to MDM.
    /// </summary>
    /// <seealso cref="Microsoft.Cloud.Metrics.Client.Configuration.IPermissionV2" />
    public sealed class DsmsAcl : IPermissionV2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DsmsAcl"/> class.
        /// </summary>
        /// <param name="identity">The DSMS ACL.</param>
        /// <param name="roleConfiguration">The role granted to this ACL.</param>
        /// <param name="description">The description of ACL.</param>
        [JsonConstructor]
        public DsmsAcl(string identity, RoleConfiguration roleConfiguration, string description)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentNullException(nameof(description));
            }

            this.Identity = identity;
            this.Description = description;
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
