// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserPermission.cs" company="Microsoft Corporation">
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
    [Obsolete]
    public sealed class UserPermission : IPermission
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserPermission"/> class.
        /// Create a new user for MDM access.
        /// </summary>
        /// <param name="identity">The alias of the user..</param>
        /// <param name="role">The role granted to this certificate.</param>
        [JsonConstructor]
        public UserPermission(string identity, Role role)
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
