// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateV2.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Certificate with access to MDM.
    /// </summary>
    /// <seealso cref="Microsoft.Cloud.Metrics.Client.Configuration.IPermissionV2" />
    public sealed class CertificateV2 : IPermissionV2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateV2"/> class.
        /// Create a new certificate for MDM access.
        /// </summary>
        /// <param name="identity">The thumbprint of the certificate in hexidecimal form.</param>
        /// <param name="roleConfiguration">The role granted to this certificate.</param>
        /// <param name="description">The description of certificate. Default value is null.</param>
        [JsonConstructor]
        public CertificateV2(string identity, RoleConfiguration roleConfiguration, string description = null)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException(nameof(identity));
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
