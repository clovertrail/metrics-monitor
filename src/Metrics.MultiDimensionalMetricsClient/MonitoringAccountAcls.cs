// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringAccountAcls.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Utility;

    /// <summary>
    /// Represents a list of ACLs associated with a monitoring account.
    /// </summary>
    /// <remarks>
    /// This does not include AP PKI as the cert is automatically generated and the ACL itself is not needed client side.
    /// </remarks>
    internal sealed class MonitoringAccountAcls : IMonitoringAccountAcls
    {
        /// <summary>
        /// Gets or sets the thumbprints.
        /// </summary>
        [JsonProperty(PropertyName = "tps")]
        public List<string> Thumbprints { get; set; }

        /// <summary>
        /// Gets or sets the dSMS acls.
        /// </summary>
        [JsonProperty(PropertyName = "dacls")]
        public List<string> DsmsAcls { get; set; }

        /// <summary>
        /// Gets or sets the KeyVault acls.
        /// </summary>
        [JsonProperty(PropertyName = "kvacls")]
        public List<string> KeyVaultAcls { get; set; }

        /// <summary>
        /// Gets the ACLs definied for the specified monitoring account
        /// </summary>
        /// <param name="accountName">Name of the account.</param>
        /// <param name="targetStampEndpoint">If not a production account, allows the target stamp to be overridden.  In most cases, allowing to default is appropriate.</param>
        /// <param name="includeReadOnly">True if the set should include those with read only access, otherwise false to return those with higher rights.</param>
        /// <returns>Acls for monitoring account.</returns>
        /// <exception cref="System.ArgumentNullException">accountName</exception>
        public static async Task<IMonitoringAccountAcls> GetAcls(string accountName, string targetStampEndpoint = "https://global.metrics.nsatc.net", bool includeReadOnly = true)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentNullException(nameof(accountName));
            }

            var client = HttpClientHelper.CreateHttpClient(TimeSpan.FromMinutes(1));
            var requestUri = $"{targetStampEndpoint}/public/monitoringAccount/{accountName}/acls?includeReadOnly={includeReadOnly}";
            var result = await HttpClientHelper.GetResponse(new Uri(requestUri), HttpMethod.Get, client, null, null).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<MonitoringAccountAcls>(result.Item1);
        }
    }
}
