// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SecurityGroup.cs" company="Microsoft Corporation">
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
    [Obsolete]
    public sealed class SecurityGroup : IPermission
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityGroup"/> class.
        /// </summary>
        /// <param name="identity">The name of the security group.</param>
        /// <param name="role">The role granted to this certificate.</param>
        [JsonConstructor]
        public SecurityGroup(string identity, Role role)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException(nameof(identity));
            }

            this.Identity = identity;
            this.Role = role;
        }

        /// <summary>
        /// The identity to grant permission.
        /// </summary>
        public string Identity { get; }

        /// <summary>
        /// The level of access to be granted to this identity.
        /// </summary>
        public Role Role { get; set; }
    }
}
