// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringAccountConfigurationManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Cloud.Metrics.Client.Utility;
    using Newtonsoft.Json;

    /// <summary>
    /// This class manages get and save operations on monitoring account configurations.
    /// </summary>
    public sealed class MonitoringAccountConfigurationManager : IMonitoringAccountConfigurationManager
    {
        private readonly ConnectionInfo connectionInfo;
        private readonly HttpClient httpClient;
        private readonly string monitoringAccountUrlPrefix;
        private readonly string tenantUrlPrefix;
        private readonly string operation;
        private readonly JsonSerializerSettings serializerSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoringAccountConfigurationManager"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information for the MDM endpoint being used.</param>
        public MonitoringAccountConfigurationManager(ConnectionInfo connectionInfo)
        {
            if (connectionInfo == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            this.connectionInfo = connectionInfo;

            this.operation = this.connectionInfo.GetAuthRelativeUrl("v1/config");

            this.monitoringAccountUrlPrefix = this.operation + "/monitoringAccount/";
            this.tenantUrlPrefix = this.operation + "/tenant/";

            this.httpClient = HttpClientHelper.CreateHttpClientWithAuthInfo(connectionInfo);

            var migrations = new[]
            {
                new ClientAssemblyMigration(
                    "Metrics.Server",
                    "Microsoft.Online.Metrics.Server.Utilities.ConfigurationUpdateResult",
                    typeof(ConfigurationUpdateResult))
            };

            this.serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Binder = new ClientAssemblyMigrationSerializationBinder(migrations)
            };
        }

        /// <summary>
        /// Get the monitoring account specified by the monitoring account name.
        /// </summary>
        /// <param name="monitoringAccountName">The name of the monitoring account.</param>
        /// <returns>The monitoring account.</returns>
        public async Task<IMonitoringAccount> GetAsync(string monitoringAccountName)
        {
            if (string.IsNullOrWhiteSpace(monitoringAccountName))
            {
                throw new ArgumentException("Monitoring account must not be blank or null.");
            }

            var path = $"{this.monitoringAccountUrlPrefix}/{monitoringAccountName}";

            // Call CheckIfGlobalEndpointWithRetry to avoid exception thrown by unresolved globalEnvrionments.
            if (this.connectionInfo.CheckIfGlobalEndpointWithRetry())
            {
                // If the global endpoint is used, account information can be retrieved from the cache.
                path += "/cache/true";
            }

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccountName))
            {
                Path = path,
                Query = "version=1"
            };

            try
            {
                var response = await HttpClientHelper.GetResponse(
                    uriBuilder.Uri,
                    HttpMethod.Get,
                    this.httpClient,
                    monitoringAccountName,
                    this.operation).ConfigureAwait(false);

                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                return JsonConvert.DeserializeObject<MonitoringAccount>(
                    response.Item1,
                    settings);
            }
            catch (MetricsClientException mce)
            {
                if (mce.ResponseStatusCode.HasValue &&
                    mce.ResponseStatusCode.Value == HttpStatusCode.NotFound)
                {
                    throw new AccountNotFoundException(
                        $"Account [{monitoringAccountName}] not found. TraceId: [{mce.TraceId}]", mce);
                }

                throw;
            }
        }

        /// <summary>
        /// Creates a monitoring account with provided configuration.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration.</param>
        /// <param name="stampHostName">The stamp name such as prod3.metrics.nsatc.net as documented @ https://aka.ms/mdm-endpoints.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task CreateAsync(IMonitoringAccount monitoringAccount, string stampHostName)
        {
            if (string.IsNullOrWhiteSpace(stampHostName))
            {
                throw new ArgumentException("value is null or empty", nameof(stampHostName));
            }

            if (this.connectionInfo.Endpoint != null)
            {
                throw new ArgumentException("'Endpoint' must not be specified in the constructor for ConnectionInfo to create monitoring accounts.");
            }

            // Check if the client has service admin permission in the global stamp; if not, try to create the account in the target stamp directly.
            var globalStampEndpoint = ConnectionInfo.ResolveGlobalEnvironments()[(int)this.connectionInfo.MdmEnvironment];
            bool hasAccountCreationPermissionInGlobalStamp = await this.HasAccountCreationPermission(globalStampEndpoint).ConfigureAwait(false);

            var endpoint = hasAccountCreationPermissionInGlobalStamp ? globalStampEndpoint : $"https://{stampHostName}/";
            var url = $"{endpoint}{this.monitoringAccountUrlPrefix}{monitoringAccount.Name}/stamp/{stampHostName}";

            await this.PostAsync(monitoringAccount, url).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new monitoring account named <paramref name="newMonitoringAccountName"/> on stamp <paramref name="stampHostName"/> by copying the common settings from <paramref name="monitoringAccountToCopyFrom" />.
        /// </summary>
        /// <param name="newMonitoringAccountName">The new monitoring account name.</param>
        /// <param name="monitoringAccountToCopyFrom">The name of the monitoring account where common settings are copied from.</param>
        /// <param name="stampHostName">The stamp name such as prod3.metrics.nsatc.net as documented @ https://aka.ms/mdm-endpoints.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task CreateAsync(string newMonitoringAccountName, string monitoringAccountToCopyFrom, string stampHostName)
        {
            if (string.IsNullOrWhiteSpace(newMonitoringAccountName))
            {
                throw new ArgumentException("value is null or empty", nameof(newMonitoringAccountName));
            }

            if (string.IsNullOrWhiteSpace(monitoringAccountToCopyFrom))
            {
                throw new ArgumentException("value is null or empty", nameof(monitoringAccountToCopyFrom));
            }

            if (string.IsNullOrWhiteSpace(stampHostName))
            {
                throw new ArgumentException("value is null or empty", nameof(stampHostName));
            }

            if (this.connectionInfo.Endpoint != null)
            {
                throw new ArgumentException("'Endpoint' must not be specified in the constructor for ConnectionInfo to create monitoring accounts.");
            }

            // Check if the client has service admin permission in the global stamp; if not, try to create the account in the target stamp directly.
            var globalStampEndpoint = ConnectionInfo.ResolveGlobalEnvironments()[(int)this.connectionInfo.MdmEnvironment];
            bool hasAccountCreationPermissionInGlobalStamp = await this.HasAccountCreationPermission(globalStampEndpoint).ConfigureAwait(false);

            var endpoint = hasAccountCreationPermissionInGlobalStamp ? globalStampEndpoint : $"https://{stampHostName}/";
            var url = $"{endpoint}{this.monitoringAccountUrlPrefix}{newMonitoringAccountName}/stamp/{stampHostName}/copy/{monitoringAccountToCopyFrom}";

            try
            {
                await HttpClientHelper.GetResponse(
                    new Uri(url),
                    HttpMethod.Post,
                    this.httpClient,
                    newMonitoringAccountName,
                    this.operation).ConfigureAwait(false);
            }
            catch (MetricsClientException mce)
            {
                ThrowSpecificExceptionIfPossible(mce, newMonitoringAccountName);
                throw;
            }
        }

        /// <summary>
        /// Save the monitoring account configuration provided.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration to save.</param>
        /// <param name="skipVersionCheck">Flag indicating whether or not the version flag should be honored.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task SaveAsync(IMonitoringAccount monitoringAccount, bool skipVersionCheck = false)
        {
            var path =
                $"{this.monitoringAccountUrlPrefix}/{monitoringAccount.Name}/skipVersionCheck/{skipVersionCheck}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount.Name))
            {
                Path = path
            };

            var url = uriBuilder.Uri.ToString();

            await this.PostAsync(monitoringAccount, url).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete the monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration to delete.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task DeleteAsync(string monitoringAccount)
        {
            var path = $"{this.tenantUrlPrefix}/{monitoringAccount}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount))
            {
                Path = path
            };

            var url = uriBuilder.Uri;

            await HttpClientHelper.GetResponse(
                url,
                HttpMethod.Delete,
                this.httpClient,
                monitoringAccount,
                this.operation).ConfigureAwait(false);
        }

        /// <summary>
        /// Un-Delete the monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration to un-delete.</param>
        /// <returns>A task the caller can wait on.</returns>
        public async Task UnDeleteAsync(string monitoringAccount)
        {
            var path = $"{this.tenantUrlPrefix}/{monitoringAccount}/undelete";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetEndpoint(monitoringAccount))
            {
                Path = path
            };

            var url = uriBuilder.Uri;

            await HttpClientHelper.GetResponse(
                url,
                HttpMethod.Post,
                this.httpClient,
                monitoringAccount,
                this.operation).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ConfigurationUpdateResult>> SyncMonitoringAccountConfigurationAsync(
            IMonitoringAccount monitoringAccount,
            bool skipVersionCheck = false)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            var mirrorOperation = $"{this.monitoringAccountUrlPrefix}replicateConfigurationToMirrorAccounts";

            var path =
                $"{this.monitoringAccountUrlPrefix}{monitoringAccount.Name}/replicateConfigurationToMirrorAccounts/skipVersionCheck/{skipVersionCheck}";

            var uriBuilder = new UriBuilder(this.connectionInfo.GetGlobalEndpoint())
            {
                Path = path
            };

            try
            {
                var response = await HttpClientHelper.GetResponse(
                    uriBuilder.Uri,
                    HttpMethod.Post,
                    this.httpClient,
                    monitoringAccount.Name,
                    mirrorOperation).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<ConfigurationUpdateResult[]>(
                    response.Item1,
                    this.serializerSettings);
            }
            catch (MetricsClientException mce)
            {
                if (mce.ResponseStatusCode == HttpStatusCode.Unauthorized)
                {
                    var exMsg = $"Unable to sync configuration for monitoring account:{monitoringAccount.Name} as "
                        + $"user doesn't have permission to update configurations. Response:{mce.Message}";

                    throw new ConfigurationValidationException(exMsg, ValidationType.ServerSide, mce);
                }

                throw;
            }
        }

        private static void ThrowSpecificExceptionIfPossible(MetricsClientException mce, string monitoringAccountName)
        {
            if (mce.ResponseStatusCode.HasValue)
            {
                switch (mce.ResponseStatusCode.Value)
                {
                    case HttpStatusCode.NotFound:
                        throw new AccountNotFoundException(
                            $"Account [{monitoringAccountName}] not found. TraceId: [{mce.TraceId}]",
                            mce);
                    case HttpStatusCode.BadRequest:
                        throw new ConfigurationValidationException(
                            $"Account [{monitoringAccountName}] could not be saved because validation failed. Response: {mce.Message}",
                            ValidationType.ServerSide,
                            mce);
                }
            }
        }

        /// <summary>
        /// Validates that the monitoring account provided can be sent to the server to be saved.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account configuration being saved.</param>
        private static void Validate(IMonitoringAccount monitoringAccount)
        {
            if (monitoringAccount == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccount));
            }

            if (string.IsNullOrWhiteSpace(monitoringAccount.Name))
            {
                throw new ArgumentException("Monitoring account name cannot be null or empty.");
            }

            if (monitoringAccount.Permissions == null || !monitoringAccount.Permissions.Any())
            {
                throw new ArgumentException("One or more permissions must be specified for this account.  These can include users, security groups, or certificates.");
            }
        }

        private async Task<bool> HasAccountCreationPermission(string stampEndpoint)
        {
            var monitoringAccount = "Monitoring account is not relevant here";
            var relativeUrl = this.connectionInfo.GetAuthRelativeUrl($"v1/config/security/writepermissions/tenant/{monitoringAccount}");
            var urlToCheckPermission = $"{stampEndpoint}/{relativeUrl}";

            Tuple<string, HttpResponseMessage> response;
            try
            {
                response = await HttpClientHelper.GetResponse(
                    new Uri(urlToCheckPermission),
                    HttpMethod.Get,
                    this.httpClient,
                    monitoringAccount,
                    this.operation).ConfigureAwait(false);
            }
            catch (MetricsClientException e)
            {
                if (e.ResponseStatusCode == HttpStatusCode.Forbidden)
                {
                    return false;
                }

                throw;
            }

            var permissions = JsonConvert.DeserializeObject<string[]>(response.Item1);
            return permissions != null && permissions.Contains("TenantConfiguration");
        }

        private async Task PostAsync(IMonitoringAccount monitoringAccount, string url)
        {
            Validate(monitoringAccount);
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var serializedMonitoringAccount = JsonConvert.SerializeObject(
                monitoringAccount,
                Formatting.Indented,
                settings);

            try
            {
                await HttpClientHelper.GetResponse(
                    new Uri(url),
                    HttpMethod.Post,
                    this.httpClient,
                    monitoringAccount.Name,
                    this.operation,
                    serializedContent: serializedMonitoringAccount).ConfigureAwait(false);
            }
            catch (MetricsClientException mce)
            {
                ThrowSpecificExceptionIfPossible(mce, monitoringAccount.Name);
                throw;
            }
        }
    }
}
