// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserAccessTokenRefresher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Cloud.Metrics.Client.Logging;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// A helper class to retrieve and refresh the user access token.
    /// </summary>
    public sealed class UserAccessTokenRefresher
    {
        /// <summary>
        /// The bearer token authentication scheme.
        /// </summary>
        public const string BearerTokenAuthScheme = "Bearer";
        private const string Authority = "https://login.microsoftonline.com/microsoft.onmicrosoft.com";
        private const string TargetResource = "http://GenevaMDM/";
        private const string ClientId = "8434f70a-4f5a-4dbe-8c83-c8ca8029ea22";
        private static readonly object LogId = Logger.CreateCustomLogId("UserAccessTokenRefresher");
        private static Lazy<UserAccessTokenRefresher> instance = new Lazy<UserAccessTokenRefresher>(() => new UserAccessTokenRefresher());
        private readonly Uri clientRedirectUri = new Uri("http://GenevaMDMClient");
        private readonly SemaphoreSlim accessTokenRefreshLock = new SemaphoreSlim(1);
        private string userAccessToken;
        private DateTime lastAccessTokenRefreshTime;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        internal static UserAccessTokenRefresher Instance
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Gets the user access token.
        /// </summary>
        internal string UserAccessToken
        {
            get
            {
                if (this.userAccessToken == null)
                {
                    this.RefreshAccessToken().Wait();

                    if (this.userAccessToken == null)
                    {
                        throw new MetricsClientException("Failed to obtain an AAD user access token.");
                    }
                }

                return this.userAccessToken;
            }
        }

        /// <summary>
        /// Refreshes the access token.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the execution.</returns>
        internal async Task RefreshAccessToken()
        {
            await this.accessTokenRefreshLock.WaitAsync();
            try
            {
                // 10-minute is an arbitrary age and we try to request the AAD token only once when multiple HTTP clients try to renew the token at the same time.
                if (this.lastAccessTokenRefreshTime < DateTime.UtcNow.AddMinutes(-10))
                {
                    var authContext = new AuthenticationContext(Authority);
                    var result =
                        await authContext.AcquireTokenAsync(TargetResource, ClientId, this.clientRedirectUri, new PlatformParameters()).ConfigureAwait(false);
                    this.userAccessToken = result.AccessToken;
                    this.lastAccessTokenRefreshTime = DateTime.UtcNow;

                    Logger.Log(LoggerLevel.Info, LogId, "RefreshAccessToken", "Succeeded");
                }
            }
            catch (Exception e)
            {
                Logger.Log(LoggerLevel.Error, LogId, "RefreshAccessToken", e.ToString());
                throw;
            }
            finally
            {
                this.accessTokenRefreshLock.Release();
            }
        }
    }
}
