// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionInfo.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Newtonsoft.Json;
    using Utility;

    /// <summary>
    /// Enumeration to distinguish the MDM environments that will be used for the connections.
    /// </summary>
    public enum MdmEnvironment
    {
        /// <summary>
        /// Uses the MDM/Jarvis production environments.
        /// </summary>
        Production,

        /// <summary>
        /// Uses the MDM/Jarvis INT environments.
        /// </summary>
        Int
    }

    /// <summary>
    /// The connection information used to connect to metrics backend.
    /// </summary>
    public sealed class ConnectionInfo
    {
        /// <summary>
        /// Possible part of operation URI.
        /// </summary>
        public const string CertApiFirstSegment = "/api/";

        /// <summary>
        /// Possible part of operation URI.
        /// </summary>
        public const string UserApiFirstSegment = "/user-api/";

        /// <summary>
        /// The default timeout.
        /// </summary>
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);

        /// <summary>
        /// There just two types of global MDM environment: INT and PROD. It must match the values defined for <see cref="MdmEnvironment"/>.
        /// </summary>
        private const int NumberOfMdmGlobalEnvironments = 2;

        /// <summary>
        /// The minimum timeout.
        /// </summary>
        private static readonly TimeSpan MinTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The maximum timeout.
        /// </summary>
        private static readonly TimeSpan MaxTimeout = TimeSpan.FromSeconds(300);

        /// <summary>
        /// The log identifier for the class.
        /// </summary>
        private static readonly object LogId = Logger.CreateCustomLogId("ConnectionInfo");

        /// <summary>
        /// Maps the accounts to the account stamp information.
        /// </summary>
        private static readonly ConcurrentDictionary<string, StampInformation>[] AccountToUriMaps = new ConcurrentDictionary<string, StampInformation>[NumberOfMdmGlobalEnvironments];

        /// <summary>
        /// Maps the GSLB to the respective Uri object according to the environment type being targeted. This avoids duplication of URIs objects
        /// if the same instance is used to connect to many different endpoints, for most usages it is expected to carry a single value.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Uri>[] GslbToUris = new ConcurrentDictionary<string, Uri>[NumberOfMdmGlobalEnvironments];

        /// <summary>
        /// The host to IP URI map.
        /// </summary>
        private static readonly ConcurrentDictionary<Uri, Uri> HostToIpUriMap = new ConcurrentDictionary<Uri, Uri>();

        /// <summary>
        /// Locks to control initialiation of statics related to a global environment.
        /// </summary>
        private static readonly object GlobalEnvironmentInitializationLock = new object();

        /// <summary>
        /// The timer to refresh accounts' home stamps.
        /// </summary>
        private static readonly Timer TimerToRefreshHomeStamp;

        /// <summary>
        /// The timer to refresh IP address.
        /// </summary>
        private static readonly Timer TimerToRefreshIpAddress;

        /// <summary>
        /// An HTTP client without authentication.
        /// </summary>
        private static readonly HttpClient HttpClientWithoutAuthentication = HttpClientHelper.CreateHttpClient(DefaultTimeout);

        /// <summary>
        /// The paths to the known global environments, its order must match the enum <see cref="MdmEnvironment"/> used to identify the desired
        /// global environment.
        /// </summary>
        private static volatile string[] globalEnvironments;

        /// <summary>
        /// The MDM environment being used, only makes sense if no <see cref="Endpoint"/> was specified in the construction of the instance.
        /// </summary>
        private readonly int mdmEnvironmentMapIndex;

        /// <summary>
        /// The certificate used to authenticate with MDM.
        /// </summary>
        private X509Certificate2 certificate;

        /// <summary>
        /// The thumbprint of the certificate used to authenticate with MDM.
        /// </summary>
        private string certificateThumbprint;

        /// <summary>
        /// Flag to tell if a global endpoint is specified.
        /// </summary>
        private bool? isGlobalEndpoint;

        /// <summary>
        /// Initializes static members of the <see cref="ConnectionInfo"/> class.
        /// </summary>
        static ConnectionInfo()
        {
            // Prefer TLS 1.2
            ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Ssl3;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            for (int index = 0; index < NumberOfMdmGlobalEnvironments; index++)
            {
                GslbToUris[index] = new ConcurrentDictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
                AccountToUriMaps[index] = new ConcurrentDictionary<string, StampInformation>(StringComparer.OrdinalIgnoreCase);
            }

            TimerToRefreshIpAddress = new Timer(state => RefreshIpAddresses(), null, DnsResolutionUpdateFrequency, System.Threading.Timeout.InfiniteTimeSpan);
            TimerToRefreshHomeStamp = new Timer(state => RefreshAccountHomeStamp(), null, HomeStampAutomaticUpdateFrequency, System.Threading.Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class for the special case of explicitly fixing the endpoint to be used.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="certificateThumbprint">The certificate thumbprint.</param>
        public ConnectionInfo(Uri endpoint, string certificateThumbprint)
            : this(endpoint, certificateThumbprint, StoreLocation.LocalMachine)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class.
        /// </summary>
        /// <param name="certificateThumbprint">The certificate thumbprint.</param>
        /// <param name="mdmEnvironment">The to be targeted by the instance.</param>
        public ConnectionInfo(string certificateThumbprint, MdmEnvironment mdmEnvironment = MdmEnvironment.Production)
            : this(null, certificateThumbprint, StoreLocation.LocalMachine, null, DefaultTimeout, mdmEnvironment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class for the special case of explicitly fixing the endpoint to be used.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="certificate">The certificate used to authenticate with MDM.</param>
        public ConnectionInfo(Uri endpoint, X509Certificate2 certificate)
            : this(endpoint, null, StoreLocation.LocalMachine, certificate, DefaultTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo" /> class.
        /// </summary>
        /// <param name="endpoint">The endpoint of metrics backend servers.</param>
        /// <param name="certificate">The certificate used to authenticate with MDM.</param>
        /// <param name="timeout">The time to wait before the request times out. The timeout value can range from 30 seconds to 5 minutes.</param>
        public ConnectionInfo(Uri endpoint, X509Certificate2 certificate, TimeSpan timeout)
            : this(endpoint, null, StoreLocation.LocalMachine, certificate, timeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class for the special case of explicitly fixing the endpoint to be used.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public ConnectionInfo(Uri endpoint)
            : this(endpoint, null, StoreLocation.LocalMachine, null, DefaultTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class for the special case of explicitly fixing the endpoint to be used.
        /// </summary>
        /// <param name="timeout">The time to wait before the request times out. The timeout value can range from 30 seconds to 5 minutes.</param>
        /// <param name="endpoint">The endpoint.</param>
        public ConnectionInfo(TimeSpan timeout, Uri endpoint)
            : this(endpoint, null, StoreLocation.LocalMachine, null, timeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class.
        /// </summary>
        /// <param name="certificate">The certificate used to authenticate with MDM.</param>
        /// <param name="mdmEnvironment">The global environment to be targeted by the instance.</param>
        public ConnectionInfo(X509Certificate2 certificate, MdmEnvironment mdmEnvironment = MdmEnvironment.Production)
            : this(null, null, StoreLocation.LocalMachine, certificate, DefaultTimeout, mdmEnvironment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class.
        /// </summary>
        /// <param name="mdmEnvironment">The global environment to be targeted by the instance.</param>
        public ConnectionInfo(MdmEnvironment mdmEnvironment = MdmEnvironment.Production)
            : this(null, null, StoreLocation.LocalMachine, null, DefaultTimeout, mdmEnvironment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class.
        /// </summary>
        /// <param name="timeout">The time to wait before the request times out. The timeout value can range from 30 seconds to 5 minutes.</param>
        /// <param name="mdmEnvironment">The global environment to be targeted by the instance.</param>
        public ConnectionInfo(TimeSpan timeout, MdmEnvironment mdmEnvironment = MdmEnvironment.Production)
            : this(null, null, StoreLocation.LocalMachine, null, timeout, mdmEnvironment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class for the special case of explicitly fixing the endpoint to be used.
        /// </summary>
        /// <param name="endpoint">The endpoint of metrics backend servers.</param>
        /// <param name="certificateThumbprint">The thumbprint of the certificate used to publish metrics data to the MDM.</param>
        /// <param name="certificateStoreLocation">The certificate store location.</param>
        public ConnectionInfo(Uri endpoint, string certificateThumbprint, StoreLocation certificateStoreLocation)
            : this(endpoint, certificateThumbprint, certificateStoreLocation, DefaultTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo"/> class.
        /// </summary>
        /// <param name="certificateThumbprint">The thumbprint of the certificate used to publish metrics data to the MDM.</param>
        /// <param name="certificateStoreLocation">The certificate store location.</param>
        /// <param name="mdmEnvironment">The global environment to be targeted by the instance.</param>
        public ConnectionInfo(string certificateThumbprint, StoreLocation certificateStoreLocation, MdmEnvironment mdmEnvironment = MdmEnvironment.Production)
            : this(null, certificateThumbprint, certificateStoreLocation, null, DefaultTimeout, mdmEnvironment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo" /> class for the special case of explicitly fixing the endpoint to be used.
        /// </summary>
        /// <param name="endpoint">The endpoint of metrics backend servers.</param>
        /// <param name="certificateThumbprint">The thumbprint of the certificate used to publish metrics data to the MDM.</param>
        /// <param name="certificateStoreLocation">The certificate store location.</param>
        /// <param name="timeout">The time to wait before the request times out. The timeout value can range from 30 seconds to 5 minutes.</param>
        public ConnectionInfo(Uri endpoint, string certificateThumbprint, StoreLocation certificateStoreLocation, TimeSpan timeout)
            : this(endpoint, certificateThumbprint, certificateStoreLocation, null, timeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo" /> class.
        /// </summary>
        /// <param name="certificateThumbprint">The thumbprint of the certificate used to publish metrics data to the MDM.</param>
        /// <param name="certificateStoreLocation">The certificate store location.</param>
        /// <param name="timeout">The time to wait before the request times out. The timeout value can range from 30 seconds to 5 minutes.</param>
        /// <param name="mdmEnvironment">The global environment to be targeted by the instance.</param>
        public ConnectionInfo(string certificateThumbprint, StoreLocation certificateStoreLocation, TimeSpan timeout, MdmEnvironment mdmEnvironment = MdmEnvironment.Production)
            : this(null, certificateThumbprint, certificateStoreLocation, null, timeout, mdmEnvironment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo" /> class for the special case of explicitly fixing the endpoint to be used.
        /// </summary>
        /// <param name="endpoint">The endpoint of metrics backend servers.</param>
        /// <param name="certificateThumbprint">The thumbprint of the certificate used to publish metrics data to the MDM.</param>
        /// <param name="certificateStoreLocation">The certificate store location.</param>
        /// <param name="certificate">The certificate used to authenticate with MDM.</param>
        /// <param name="timeout">The time to wait before the request times out. The timeout value can range from 30 seconds to 5 minutes.</param>
        /// <exception cref="ArgumentNullException">
        /// endpoint
        /// or
        /// certificateThumbprint
        /// </exception>
        private ConnectionInfo(Uri endpoint, string certificateThumbprint, StoreLocation certificateStoreLocation, X509Certificate2 certificate, TimeSpan timeout)
            : this(endpoint, certificateThumbprint, certificateStoreLocation, certificate, timeout, MdmEnvironment.Production)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInfo" /> class.
        /// </summary>
        /// <param name="endpoint">The endpoint of metrics backend servers.</param>
        /// <param name="certificateThumbprint">The thumbprint of the certificate used to publish metrics data to the MDM.</param>
        /// <param name="certificateStoreLocation">The certificate store location.</param>
        /// <param name="certificate">The certificate used to authenticate with MDM.</param>
        /// <param name="timeout">The time to wait before the request times out. The timeout value can range from 30 seconds to 5 minutes.</param>
        /// <param name="mdmEnvironment">The global environment to be targeted by the instance.</param>
        /// <exception cref="ArgumentNullException">
        /// endpoint
        /// or
        /// certificateThumbprint
        /// </exception>
        private ConnectionInfo(
            Uri endpoint,
            string certificateThumbprint,
            StoreLocation certificateStoreLocation,
            X509Certificate2 certificate,
            TimeSpan timeout,
            MdmEnvironment mdmEnvironment)
        {
            if (certificate != null && !string.IsNullOrWhiteSpace(certificateThumbprint))
            {
                throw new ArgumentException($"Either {nameof(certificate)} or {nameof(certificateThumbprint)} can be specified, but not both.");
            }

            if (timeout < MinTimeout || timeout > MaxTimeout)
            {
                throw new ArgumentException($"The timeout value for a request must be between {MinTimeout} and {MaxTimeout}.");
            }

            if (certificate == null && string.IsNullOrWhiteSpace(certificateThumbprint))
            {
                this.UseAadUserAuthentication = true;
            }

            Logger.Log(
                LoggerLevel.Info,
                LogId,
                "Created",
                "A new connection was created. Endpoint:{0}, CertThumbprint:{1}, CertStore:{2}, TimeoutMs:{3}",
                endpoint,
                certificate != null ? certificate.Thumbprint : certificateThumbprint,
                certificateStoreLocation,
                timeout.TotalMilliseconds);

            this.Endpoint = endpoint;

            this.CertificateThumbprint = certificateThumbprint;
            this.CertificateStoreLocation = certificateStoreLocation;
            this.Certificate = certificate;
            this.Timeout = timeout;

            if (mdmEnvironment != MdmEnvironment.Production && mdmEnvironment != MdmEnvironment.Int)
            {
                throw new ArgumentException($"The parameter {nameof(mdmEnvironment)} has an invalid value {mdmEnvironment}");
            }

            this.mdmEnvironmentMapIndex = (int)mdmEnvironment;
        }

        /// <summary>
        /// Gets or sets the frequency in which the automatic refresh of home stamps is going to be performed (if any refresh is being performed).
        /// </summary>
        public static TimeSpan HomeStampAutomaticUpdateFrequency { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the frequency in which the automatic refresh of home stamp host to IP map is going to be performed.
        /// </summary>
        public static TimeSpan DnsResolutionUpdateFrequency { get; set; } = TimeSpan.FromSeconds(4);

        /// <summary>
        /// Gets a value indicating whether to use AAD user authentication.
        /// </summary>
        public bool UseAadUserAuthentication { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is global endpoint.
        /// </summary>
        public bool IsGlobalEndpoint
        {
            get
            {
                if (this.Endpoint == null)
                {
                    return false;
                }

                if (this.isGlobalEndpoint.HasValue)
                {
                    return this.isGlobalEndpoint.Value;
                }

                if (globalEnvironments == null)
                {
                    throw new MetricsClientException("The global environments haven't been resolved yet.");
                }

                foreach (var ge in globalEnvironments)
                {
                    if (this.Endpoint.Host.Equals(new Uri(ge).Host, StringComparison.OrdinalIgnoreCase))
                    {
                        this.isGlobalEndpoint = true;
                        break;
                    }
                }

                if (this.isGlobalEndpoint != true)
                {
                    this.isGlobalEndpoint = false;
                }

                return this.isGlobalEndpoint.Value;
            }
        }

        /// <summary>
        /// Gets or sets the additional default request headers used for internal testing.
        /// </summary>
        public Dictionary<string, string> AdditionalDefaultRequestHeaders { get; set; }

        /// <summary>
        /// Gets the endpoint pointed by this instance, typically null, only not null when using an instance .
        /// </summary>
        public Uri Endpoint { get; }

        /// <summary>
        /// Gets the certificate used to authenticate with MDM.
        /// </summary>
        public X509Certificate2 Certificate
        {
            get
            {
                if (this.certificate == null)
                {
                    this.certificate = CertificateHelper.FindAndValidateCertificate(this.CertificateThumbprint, this.CertificateStoreLocation);
                }

                return this.certificate;
            }

            private set
            {
                this.certificate = value;
            }
        }

        /// <summary>
        /// Gets the certificate thumbprint.
        /// </summary>
        public string CertificateThumbprint
        {
            get
            {
                if (this.certificate != null)
                {
                    this.certificateThumbprint = this.certificate.Thumbprint;
                }

                return this.certificateThumbprint;
            }

            private set
            {
                this.certificateThumbprint = value;
            }
        }

        /// <summary>
        /// Gets the MDM stamp being used by the connection object.
        /// </summary>
        public MdmEnvironment MdmEnvironment
        {
            get
            {
                if (this.Endpoint != null)
                {
                    throw new InvalidOperationException($"Endpoint was specified during construction of instance, this instance operates only against that given endpoint {this.Endpoint}.");
                }

                return (MdmEnvironment)this.mdmEnvironmentMapIndex;
            }
        }

        /// <summary>
        /// Gets the certificate store location.
        /// </summary>
        public StoreLocation CertificateStoreLocation { get; private set; }

        /// <summary>
        /// Gets the time to wait before the request times out.
        /// </summary>
        public TimeSpan Timeout { get; private set;  }

        /// <summary>
        /// Gets or sets a value indicating whether to disable DNS resolution for unit test.
        /// </summary>
        internal static bool DisableDnsResolutionForUnitTest { get; set; }

        /// <summary>
        /// Gets the endpoint for a given MDM monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account for which we want to retrieve the endpoint.</param>
        /// <returns>Returns the URI of the given account, or the fixed endpoint if one was specified at construction time.</returns>
        public Uri GetEndpoint(string monitoringAccount)
        {
            if (this.Endpoint != null)
            {
                // This instance is fixed on a endpoint always return that endpoint.
                return this.Endpoint;
            }

            StampInformation endpoint = this.GetAndUpdateIfRequiredStampInformation(monitoringAccount);
            return endpoint.StampMainUri;
        }

        /// <summary>
        /// Gets the endpoint for querying metrics data information of MDM monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account for which we want to retrieve the endpoint.</param>
        /// <returns>Returns the URI of the given account, or the fixed endpoint if one was specified at construction time.</returns>
        public Uri GetMetricsDataQueryEndpoint(string monitoringAccount)
        {
            if (this.Endpoint != null)
            {
                // This instance is fixed on a endpoint always return that endpoint.
                return this.Endpoint;
            }

            if (this.UseAadUserAuthentication)
            {
                return this.GetEndpoint(monitoringAccount);
            }

            StampInformation endpoint = this.GetAndUpdateIfRequiredStampInformation(monitoringAccount);
            return endpoint.StampQueryUri;
        }

        /// <summary>
        /// Gets the global endpoint.
        /// </summary>
        /// <returns>Returns the URI of the global endpoint.</returns>
        public Uri GetGlobalEndpoint()
        {
            return new Uri(ResolveGlobalEnvironments()[this.mdmEnvironmentMapIndex]);
        }

        /// <summary>
        /// Resolves the base URIs to the known global environments, and its order must match the enum <see cref="MdmEnvironment" /> used to identify the desired
        /// global environment.
        /// </summary>
        /// <returns>The base URIs to the known global environments.</returns>
        internal static string[] ResolveGlobalEnvironments()
        {
            if (globalEnvironments != null)
            {
                return globalEnvironments;
            }

            lock (GlobalEnvironmentInitializationLock)
            {
                globalEnvironments = new[]
                {
                    $"https://{ProductionGlobalEnvironmentResolver.ResolveGlobalStampHostName()}",
                    "https://az-int.metrics.nsatc.net"
                };

                for (int i = 0; i < globalEnvironments.Length; i++)
                {
                    // Some accounts may have empty home gslb set in that case use the default stamp for the environment.
                    var defaultGlobalUri = new Uri(globalEnvironments[i]);
                    GslbToUris[i].TryAdd(string.Empty, defaultGlobalUri);
                }

                return globalEnvironments;
            }
        }

        /// <summary>
        /// Resolves the ip for <paramref name="hostname"/>.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="throwOnFailure">if set to <c>true</c> [throw on failure].</param>
        /// <returns>The IPv4 address.</returns>
        internal static async Task<string> ResolveIp(string hostname, bool throwOnFailure)
        {
            try
            {
                IPAddress[] addresslist = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);

                // use only IPv4 for now.
                var resolvedIp = addresslist?.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString();
                if (string.IsNullOrWhiteSpace(resolvedIp))
                {
                    throw new Exception($"Resolved IP is null or empty. Addresslist:{JsonConvert.SerializeObject(addresslist)}.");
                }

                return resolvedIp;
            }
            catch (Exception e)
            {
                Logger.Log(
                    LoggerLevel.Error,
                    LogId,
                    "ResolveIp",
                    $"Resolving hostname to IP got an exception. HostName:{hostname}, Exception:{e}");

                if (throwOnFailure)
                {
                    throw new MetricsClientException($"Resolving {hostname} to IP got an exception.", e);
                }

                return null;
            }
        }

        /// <summary>
        /// Attempts to get the IP address from the cache. If unable it will resolve the IP and cache the value prior to returning.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The ip url for the endpoint.</returns>
        internal static async Task<Uri> GetCachedIpAddress(Uri endpoint)
        {
            if (DisableDnsResolutionForUnitTest)
            {
                return endpoint;
            }

            if (endpoint.Host == "metrics-ob.dc.ad.msft.net")
            {
                // TODO: it must be some devbox setup issue that caused 127.0.0.1:44300 not to work.
                return endpoint;
            }

            Uri ipUri;
            if (HostToIpUriMap.TryGetValue(endpoint, out ipUri))
            {
                return ipUri;
            }

            var resolvedIp = await ResolveIp(endpoint.Host, throwOnFailure: true).ConfigureAwait(false);
            ipUri = new Uri($"https://{resolvedIp}:{endpoint.Port}");
            HostToIpUriMap.TryAdd(endpoint, ipUri);
            return ipUri;
        }

        /// <summary>
        /// Gets the relative URL depending on authentication.
        /// </summary>
        /// <param name="relativeUrl">The relative URL without authentication consideration.</param>
        /// <returns>The relative URL considering authentication type.</returns>
        internal string GetAuthRelativeUrl(string relativeUrl)
        {
           return this.UseAadUserAuthentication ? UserApiFirstSegment + relativeUrl : CertApiFirstSegment + relativeUrl;
        }

        /// <summary>
        /// Check this instance's field <see cref="isGlobalEndpoint" /> by handling exception with retry.
        /// </summary>
        /// <returns>The value of <see cref="isGlobalEndpoint" /> with retry.</returns>
        internal bool CheckIfGlobalEndpointWithRetry()
        {
            try
            {
                return this.IsGlobalEndpoint;
            }
            catch (MetricsClientException)
            {
            }

            ResolveGlobalEnvironments();
            return this.IsGlobalEndpoint;
        }

        private static async void RefreshIpAddresses()
        {
            try
            {
                Logger.Log(
                    LoggerLevel.Info,
                    LogId,
                    "RefreshIpAddresses",
                    "Initiated the automatic refresh of IP addresses of home stamp endpoints.");

                if (globalEnvironments == null)
                {
                    ResolveGlobalEnvironments();
                }

                foreach (var kvp in HostToIpUriMap)
                {
                    var hostname = kvp.Key.Host;
                    var resolvedIp = await ResolveIp(hostname, throwOnFailure: false).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(resolvedIp))
                    {
                        var existingIp = kvp.Value.Host;
                        if (resolvedIp != existingIp)
                        {
                            HostToIpUriMap.TryUpdate(kvp.Key, new Uri($"https://{resolvedIp}:{kvp.Key.Port}"), kvp.Value);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(LoggerLevel.Error, LogId, "RefreshIpAddresses", $"Hit exception: {e}");
            }
            finally
            {
                if (DnsResolutionUpdateFrequency == TimeSpan.Zero)
                {
                    Logger.Log(LoggerLevel.Error, LogId, "RefreshIpAddresses", "Terminate the refresh task since the refresh frequency is 0");
                }
                else
                {
                    TimerToRefreshIpAddress.Change(DnsResolutionUpdateFrequency, System.Threading.Timeout.InfiniteTimeSpan);
                }
            }
        }

        /// <summary>
        /// Helper method used to set-up the map from account to home stamp URI when endpoint is not explicitly set. This method
        /// is intended to be called from an instance constructor and it will use the certificate of that instance to retrieve the
        /// home stamp URI from the respective global environment. This method should throw any exception that it receives while attempting
        /// to build the home stamp map for the accounts.
        /// </summary>
        private static async void RefreshAccountHomeStamp()
        {
            if (globalEnvironments == null)
            {
                Logger.Log(LoggerLevel.Error, LogId, "RefreshAccountHomeStamp", "The global environments haven't been resolved yet.");
                return;
            }

            for (int index = 0; index < NumberOfMdmGlobalEnvironments; index++)
            {
                try
                {
                    await UpdateAccountToUriMapAsync(HttpClientWithoutAuthentication, globalEnvironments[index], GslbToUris[index], AccountToUriMaps[index]).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Log(
                        LoggerLevel.Error,
                        LogId,
                        "RefreshAccountHomeStamp",
                        $"The periodic background task that updates the account to URI map got an exception. Environment:{globalEnvironments[index]}, Exception:{e}");
                }
            }

            if (HomeStampAutomaticUpdateFrequency == TimeSpan.Zero)
            {
                Logger.Log(LoggerLevel.Error, LogId, "RefreshAccountHomeStamp", "Terminate the home stamp refresh task since the refresh frequency is 0");
            }
            else
            {
                TimerToRefreshHomeStamp.Change(HomeStampAutomaticUpdateFrequency, System.Threading.Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Helper method that updates the targeted map with the most recent information from the given global stamp.
        /// </summary>
        /// <param name="httpClient">The HTTP client helper to be used to communicate with the global stamp.</param>
        /// <param name="globalEnvironmentUrl">The request URL to be used on the request to retrieve the accounts.</param>
        /// <param name="gslbToUris">A dictionary that maps home gslb strings to Uri objects to avoid duplicate Uri objects.</param>
        /// <param name="targetMap">The map to be updated with the results of the method run.</param>
        /// <returns>
        /// A task that can be used to synchronize the completion of the async method.
        /// </returns>
        private static async Task UpdateAccountToUriMapAsync(
            HttpClient httpClient,
            string globalEnvironmentUrl,
            ConcurrentDictionary<string, Uri> gslbToUris,
            ConcurrentDictionary<string, StampInformation> targetMap)
        {
            Logger.Log(
                LoggerLevel.Info,
                LogId,
                "UpdateAccountToUriMapAsync",
                "Initiated the automatic refresh of the account home stamp endpoints.");

            foreach (var account in targetMap.Keys)
            {
                try
                {
                    await GetAndUpdateStampInfoAsync(httpClient, globalEnvironmentUrl, gslbToUris, targetMap, account).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // Exception treated for each call so a single bad/corrupt account does not preclude the update of the others. It is possible
                    // that this will log a few accounts in case of lost connection but it seems preferable to have a single account failure (think about
                    // a single account failing constantly) blocking the update of others.
                    Logger.Log(
                        LoggerLevel.Error,
                        LogId,
                        "AccountToUriUpdate",
                        "The periodic background task that updates the account to URI map got an exception for account {0}. URL:{1}, Exception:{2}",
                        account,
                        globalEnvironmentUrl,
                        e);
                }
            }
        }

        /// <summary>
        /// Helper method that gets the URI for a given account and updates the mapping from account to home stamp.
        /// </summary>
        /// <param name="httpClient">Helper object to be used in the operation.</param>
        /// <param name="globalStampUrl">Global stamp to be targeted in the operation.</param>
        /// <param name="gslbToUris">Stores all created home stamp URI objects in order to avoid duplications.</param>
        /// <param name="targetMap">Map to be updated with the latest information retrived remotely.</param>
        /// <param name="account">Account for which the URI should be retrieved.</param>
        /// <returns>
        /// The URI for the requested account.
        /// </returns>
        private static async Task<StampInformation> GetAndUpdateStampInfoAsync(HttpClient httpClient, string globalStampUrl, ConcurrentDictionary<string, Uri> gslbToUris, ConcurrentDictionary<string, StampInformation> targetMap, string account)
        {
            var requestUrl = $"{globalStampUrl}/public/monitoringAccount/{account}/homeStamp";
            var response = await HttpClientHelper.GetResponse(new Uri(requestUrl), HttpMethod.Get, httpClient, null, null).ConfigureAwait(false);
            var homeStampGslbHostname = JsonConvert.DeserializeObject<string>(response.Item1);
            string queryEndpoint = null;

            if (!string.IsNullOrEmpty(homeStampGslbHostname))
            {
                queryEndpoint = await SafeGetStampMetricDataQueryEndpointHostNameAsync(httpClient, "https://" + homeStampGslbHostname).ConfigureAwait(false);
            }

            Uri homeGslbUri;
            var homeGslbKey = string.IsNullOrWhiteSpace(homeStampGslbHostname) ? string.Empty : homeStampGslbHostname;
            if (!gslbToUris.TryGetValue(homeGslbKey, out homeGslbUri))
            {
                homeGslbUri = new Uri("https://" + homeStampGslbHostname);
                gslbToUris.AddOrUpdate(homeGslbKey, homeGslbUri, (_, uri) => homeGslbUri);
            }

            Uri queryEndpointUri;
            if (!string.IsNullOrEmpty(queryEndpoint))
            {
                queryEndpointUri = new Uri("https://" + queryEndpoint);
            }
            else
            {
                queryEndpointUri = homeGslbUri;
            }

            var stampInfo = new StampInformation(homeGslbUri, queryEndpointUri);
            targetMap.AddOrUpdate(
                account,
                stampInfo,
                (_, uri) => stampInfo);

            return stampInfo;
        }

        /// <summary>
        /// Helper method that gets the metric data query endpoint for the given mdm stamp.
        /// Return string.empty if no seperate query endpoint is configured.
        /// </summary>
        /// <param name="httpClient">Helper object to be used in the operation.</param>
        /// <param name="stampUrl">Url of the mdm stamp for which query endpoint needs to be discovered.</param>
        /// <returns>
        /// The URI for the requested account.
        /// </returns>
        private static async Task<string> SafeGetStampMetricDataQueryEndpointHostNameAsync(HttpClient httpClient, string stampUrl)
        {
            try
            {
                string requestUrl = $"{stampUrl}/public/metricsDataQueryEndpointHostName";
                Tuple<string, HttpResponseMessage> response = await HttpClientHelper.GetResponse(new Uri(requestUrl), HttpMethod.Get, httpClient, null, null).ConfigureAwait(false);
                var queryEndpoint = JsonConvert.DeserializeObject<string>(response.Item1);

                return queryEndpoint;
            }
            catch (Exception exp)
            {
                var httpException = exp as MetricsClientException;
                if (httpException?.ResponseStatusCode == HttpStatusCode.NotFound)
                {
                    Logger.Log(
                        LoggerLevel.Info,
                        LogId,
                        nameof(SafeGetStampMetricDataQueryEndpointHostNameAsync),
                        "Failed to resolve query endpoint for the mdm stamp due to query resolution endpoint not found, will use main endpoint.");
                }
                else
                {
                    Logger.Log(
                        LoggerLevel.Warning,
                        LogId,
                        nameof(SafeGetStampMetricDataQueryEndpointHostNameAsync),
                        "Failed to resolve query endpoint for the mdm stamp due to unexpected exception, will use main endpoint.",
                        exp);
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the endpoint for querying metrics data information of MDM monitoring account.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account for which we want to retrieve the endpoint.</param>
        /// <returns>Returns the URI of the given account, or the fixed endpoint if one was specified at construction time.</returns>
        private StampInformation GetAndUpdateIfRequiredStampInformation(string monitoringAccount)
        {
            StampInformation endpoint;
            if (!AccountToUriMaps[this.mdmEnvironmentMapIndex].TryGetValue(monitoringAccount, out endpoint))
            {
                // In this case we need to block until operation is completed since this is the information requested by the user.
                endpoint = GetAndUpdateStampInfoAsync(
                    HttpClientWithoutAuthentication,
                    ResolveGlobalEnvironments()[this.mdmEnvironmentMapIndex],
                    GslbToUris[this.mdmEnvironmentMapIndex],
                    AccountToUriMaps[this.mdmEnvironmentMapIndex],
                    monitoringAccount).GetAwaiter().GetResult();
            }

            return endpoint;
        }

        private struct StampInformation
        {
            public StampInformation(Uri stampMainUri, Uri stampQueryUri)
            {
                this.StampMainUri = stampMainUri;
                this.StampQueryUri = stampQueryUri;
            }

            public Uri StampMainUri { get; }

            public Uri StampQueryUri { get; }
        }
    }
}
