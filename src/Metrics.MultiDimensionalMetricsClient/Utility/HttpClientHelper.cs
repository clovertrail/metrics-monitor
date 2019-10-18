// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpClientHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Security;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Newtonsoft.Json;
    using Online.Metrics.Serialization;

    /// <summary>
    /// The http client helper class.
    /// </summary>
    public static class HttpClientHelper
    {
        /// <summary>
        /// The HTTP status code for too many requests.
        /// </summary>
        /// <remarks>
        /// Refer: http://tools.ietf.org/html/rfc6585.
        /// </remarks>
        public const int HttpStatusCodeThrottled = 429;

        /// <summary>
        /// The white listed server subject names.
        /// </summary>
        public static readonly HashSet<string> WhiteListedServerSubjectNames = new HashSet<string>(new[] { "CN=*.dc.ad.msft.net", "CN=*.test.dc.ad.msft.net", "CN=*.ff.dc.ad.msft.net", "CN=*.test.ff.dc.ad.msft.net", "CN=*.cn.dc.ad.msft.net, O=Shanghai Blue Cloud Technology Co. Ltd, L=Shanghai, C=CN" });

        /// <summary>
        /// The default maximum web requests per minute
        /// </summary>
        private const int DefaultMaxWebRequestsPerMinute = 1000;

        /// <summary>
        /// The throttled identity http header name.
        /// </summary>
        private const string ThrottledIdentityKey = "Throttled-Identity";

        /// <summary>
        /// The retry after http header name.
        /// </summary>
        private const string RetryAfterKey = "Retry-After";

        /// <summary>
        /// The version of the current metrics client assembly
        /// </summary>
        private static readonly string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// The log identifier for this class
        /// </summary>
        private static readonly object LogId = Logger.CreateCustomLogId("HttpClientHelper");

        /// <summary>
        /// The identities throttled by server.
        /// Keys are monitoring accounts.
        /// Values are throttled identities to DateTime to proceed sending requests.
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateTime>> ServerThrottledIdentities;

        /// <summary>
        /// The current minute of the hour
        /// </summary>
        private static volatile int currentMinute = DateTime.UtcNow.Minute;

        /// <summary>
        /// The requests sent in current minute
        /// </summary>
        private static int requestsSentInCurrentMinute;

        /// <summary>
        /// Initializes static members of the <see cref="HttpClientHelper"/> class.
        /// </summary>
        static HttpClientHelper()
        {
            EnableHttpPipelining = false;
            ServerThrottledIdentities = new ConcurrentDictionary<string, ConcurrentDictionary<string, DateTime>>();
        }

        /// <summary>
        /// Gets or sets the max web requests per minute.
        /// </summary>
        public static ushort MaxWebRequestsPerMinute { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether not to set certificate validation callback so that consumers control it.
        /// </summary>
        public static bool DoNotSetCertificateValidationCallback { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if http pipelining should be enabled or not for http(s) requests.
        ///
        /// This is disabled by default.
        /// </summary>
        public static bool EnableHttpPipelining { get; set; }

        /// <summary>
        /// Gets the authentication header
        /// </summary>
        /// <returns>The authentication header</returns>
        public static AuthenticationHeaderValue GetAuthenticationHeader()
        {
            return new AuthenticationHeaderValue(UserAccessTokenRefresher.BearerTokenAuthScheme, UserAccessTokenRefresher.Instance.UserAccessToken);
        }

        /// <summary>
        /// Creates the HTTP client with user or cert authentication.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="authHeaderValue">The authentication header value.</param>
        /// <returns>An instance of <see cref="HttpClient"/></returns>
        public static HttpClient CreateHttpClientWithAuthInfo(ConnectionInfo connectionInfo, string authHeaderValue = null)
        {
            HttpClient httpClient;
            var webRequestHandler = new HttpClientHandler { AllowAutoRedirect = false, };
            if (!DoNotSetCertificateValidationCallback)
            {
                webRequestHandler.ServerCertificateCustomValidationCallback = CertificateValidationCallback;
            }

            if (connectionInfo.UseAadUserAuthentication)
            {
                httpClient = new HttpClient(webRequestHandler, disposeHandler: true);

                AuthenticationHeaderValue authHeader;
                if (authHeaderValue == null)
                {
                    authHeader = GetAuthenticationHeader();
                }
                else
                {
                    authHeader = new AuthenticationHeaderValue(UserAccessTokenRefresher.BearerTokenAuthScheme, authHeaderValue);
                }

                httpClient.DefaultRequestHeaders.Authorization = authHeader;
            }
            else
            {
                webRequestHandler.ClientCertificates.Add(connectionInfo.Certificate);
                httpClient = new HttpClient(webRequestHandler, disposeHandler: true);
            }

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(SerializationConstants.OctetStreamContentType));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MultiDimensionalMetricsClient");
            httpClient.DefaultRequestHeaders.Add("MultiDimensionalMetricsClientVersion", AssemblyVersion);

            if (connectionInfo.AdditionalDefaultRequestHeaders != null)
            {
                foreach (var kvp in connectionInfo.AdditionalDefaultRequestHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
                }
            }

            httpClient.Timeout = connectionInfo.Timeout;

            Logger.Log(
                LoggerLevel.Info,
                LogId,
                "CreateClient",
                "Created new HttpClient. CertThumbprint:{0}, TimeoutMs:{1}",
                connectionInfo.CertificateThumbprint,
                connectionInfo.Timeout.TotalMilliseconds);

            return httpClient;
        }

        /// <summary>
        /// Creates the HTTP client
        /// </summary>
        /// <param name="timeout">The timeout to apply to the requests.</param>
        /// <returns>
        /// An instance of <see cref="HttpClient" />
        /// </returns>
        public static HttpClient CreateHttpClient(TimeSpan timeout)
        {
            var webRequestHandler = new HttpClientHandler { AllowAutoRedirect = false };
            if (!DoNotSetCertificateValidationCallback)
            {
                webRequestHandler.ServerCertificateCustomValidationCallback = CertificateValidationCallback;
            }

            HttpClient httpClient = new HttpClient(webRequestHandler, disposeHandler: true);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(SerializationConstants.OctetStreamContentType));

            httpClient.Timeout = timeout;

            Logger.Log(
                LoggerLevel.Info,
                LogId,
                "CreateClient",
                "Created new HttpClient. TimeoutMs:{0}",
                timeout.TotalMilliseconds);

            return httpClient;
        }

        /// <summary>
        /// Callback when a server side certificate is validated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors.</param>
        /// <returns>True if no errors found while validation.</returns>
        public static bool CertificateValidationCallback(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // MDM is using 3rd party domains, metrics.nsatc.net, to reduce the dependency on MSFT infrasture, so we cannot obtain a certificate matching the domains.
            // We will consider moving to MSFT domains but for now we customize the validation on cert subject name for requests toward MDM stamp hostnames or IPs.
            return sslPolicyErrors == SslPolicyErrors.None
                   || (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch && WhiteListedServerSubjectNames.Contains(certificate.Subject));
        }

        /// <summary>
        /// Adds the standard headers to message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="traceId">The trace identifier that will be placed in a header.</param>
        /// <param name="sourceIdentity">The source identity that will be placed in a header.</param>
        /// <param name="hostname">The host name of an endpoint.</param>
        public static void AddStandardHeadersToMessage(HttpRequestMessage message, Guid traceId, string sourceIdentity, string hostname)
        {
            message.Headers.Add(SerializationConstants.TraceIdHeader, traceId.ToString("B"));
            message.Headers.Add("SourceIdentity", sourceIdentity);
            message.Headers.Host = hostname;
        }

        /// <summary>
        /// Gets the response as string.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="method">The http method.</param>
        /// <param name="client">The HTTP client.</param>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="operation">The operation.</param>
        /// <param name="httpContent">Content of the HTTP request.</param>
        /// <param name="clientId">Optional parameter identifying client.</param>
        /// <param name="serializedContent">Serialized content of the HTTP request, if special serialization is needed.</param>
        /// <param name="traceId">The trace identifier.</param>
        /// <param name="serializationVersion">The serialization version.</param>
        /// <param name="numAttempts">The number of attempts.</param>
        /// <returns>
        /// The HTTP response message as a string.
        /// </returns>
        /// <remarks>
        /// We attempt up to 3 times with delay of 5 seconds and 10 seconds in between respectively, if the request cannot be sent or the response status code is 503.
        /// However, we don't want to retry in the OBO case.
        /// </remarks>
        public static async Task<string> GetResponseAsStringAsync(
            Uri url,
            HttpMethod method,
            HttpClient client,
            string monitoringAccount,
            string operation,
            object httpContent = null,
            string clientId = "",
            string serializedContent = null,
            Guid? traceId = null,
            byte serializationVersion = MetricQueryResponseDeserializer.CurrentVersion,
            int numAttempts = 3)
        {
            Tuple<string, HttpResponseMessage> response = null;
            try
            {
                response = await GetResponse(
                        url,
                        method,
                        client,
                        monitoringAccount,
                        operation,
                        httpContent,
                        clientId,
                        serializedContent,
                        traceId,
                        serializationVersion,
                        numAttempts)
                    .ConfigureAwait(false);

                return response.Item1;
            }
            finally
            {
                response?.Item2?.Dispose();
            }
        }

        /// <summary>
        /// Gets the HTTP response message as a string.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="method">The http method.</param>
        /// <param name="client">The HTTP client.</param>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="operation">The operation.</param>
        /// <param name="httpContent">Content of the HTTP request.</param>
        /// <param name="clientId">Optional parameter identifying client.</param>
        /// <param name="serializedContent">Serialized content of the HTTP request, if special serialization is needed.</param>
        /// <param name="traceId">The trace identifier.</param>
        /// <param name="serializationVersion">The serialization version.</param>
        /// <param name="numAttempts">The number of attempts.</param>
        /// <returns>
        /// The HTTP response message as a string.
        /// </returns>
        /// <remarks>
        /// We attempt up to 3 times with delay of 5 seconds and 10 seconds in between respectively, if the request cannot be sent or the response status code is 503.
        /// However, we don't want to retry in the OBO case.
        /// </remarks>
        public static async Task<Tuple<string, HttpResponseMessage>> GetResponse(
            Uri url,
            HttpMethod method,
            HttpClient client,
            string monitoringAccount,
            string operation,
            object httpContent = null,
            string clientId = "",
            string serializedContent = null,
            Guid? traceId = null,
            byte serializationVersion = MetricQueryResponseDeserializer.CurrentVersion,
            int numAttempts = 3)
        {
            const int baseWaitTimeInSeconds = 5;
            Exception lastException = null;

            if (IsThrottledByServer(monitoringAccount, operation, method))
            {
                throw new MetricsClientException(
                    $"The request is throttled by the server. Url:{url}, Method:{method}, Operation:{operation}.",
                    null,
                    traceId ?? Guid.Empty,
                    (HttpStatusCode)HttpStatusCodeThrottled);
            }

            var stopWatch = Stopwatch.StartNew();
            for (int i = 1; i <= numAttempts; i++)
            {
                try
                {
                    return await GetResponseWithTokenRefresh(url, method, client, httpContent, clientId, serializedContent, traceId, serializationVersion, monitoringAccount).ConfigureAwait(false);
                }
                catch (MetricsClientException e)
                {
                    lastException = e;

                    if (stopWatch.Elapsed >= client.Timeout ||
                        (e.ResponseStatusCode != null && e.ResponseStatusCode != HttpStatusCode.ServiceUnavailable) ||
                        i == numAttempts)
                    {
                        throw;
                    }

                    var delay = TimeSpan.FromSeconds(baseWaitTimeInSeconds * i);

                    Logger.Log(LoggerLevel.Info, LogId, "GetResponse", "Delay {0} and then retry.", delay);
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }

            throw new MetricsClientException($"Exhausted {numAttempts} attempts.", lastException);
        }

        private static async Task<Tuple<string, HttpResponseMessage>> GetResponseWithTokenRefresh(
            Uri url,
            HttpMethod method,
            HttpClient client,
            object httpContent,
            string clientId,
            string serializedContent,
            Guid? traceId,
            byte serializationVersion,
            string monitoringAccount)
        {
            try
            {
                return await GetResponseNoRetry(url, method, client, httpContent, clientId, serializedContent, traceId, serializationVersion, monitoringAccount).ConfigureAwait(false);
            }
            catch (MetricsClientException e)
            {
                if (e.ResponseStatusCode == HttpStatusCode.Redirect)
                {
                    await UserAccessTokenRefresher.Instance.RefreshAccessToken().ConfigureAwait(false);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(UserAccessTokenRefresher.BearerTokenAuthScheme, UserAccessTokenRefresher.Instance.UserAccessToken);
                    return await GetResponseNoRetry(url, method, client, httpContent, clientId, serializedContent, traceId, serializationVersion, monitoringAccount).ConfigureAwait(false);
                }

                throw;
            }
        }

        private static async Task<Tuple<string, HttpResponseMessage>> GetResponseNoRetry(
            Uri url,
            HttpMethod method,
            HttpClient client,
            object httpContent,
            string clientId,
            string serializedContent,
            Guid? traceId,
            byte serializationVersion,
            string monitoringAccount)
        {
            const string LogTag = "GetResponse";

            traceId = traceId ?? Guid.NewGuid();

            HandleGeneralClientThrottling(url, method, traceId.Value);
            var ipUrlBuilder = new UriBuilder(url)
            {
                Host = (await ConnectionInfo.GetCachedIpAddress(url).ConfigureAwait(false)).Host
            };

            var request = new HttpRequestMessage(method, ipUrlBuilder.Uri);
            var sourceId = Environment.MachineName;
            AddStandardHeadersToMessage(request, traceId.Value, sourceId, url.Host);
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                request.Headers.Add(SerializationConstants.ClientIdHeader, clientId);
            }

            if (serializationVersion == 1)
            {
                // We won't use this header for new versions of serialization.
                request.Headers.Add(SerializationConstants.ScalingFactorDisabledHeader, "true");
            }

            if (httpContent != null && serializedContent == null)
            {
                serializedContent = JsonConvert.SerializeObject(httpContent);
            }

            if (serializedContent != null)
            {
                request.Content = new StringContent(serializedContent, Encoding.UTF8, "application/json");
            }

            Logger.Log(
                LoggerLevel.Info,
                LogId,
                LogTag,
                "Making HTTP request. TraceId:{0}, Url:{1}, Method:{2}, SourceId:{3}, ContentLength:{4}, DnsTimeoutMs:{5}, TimeoutMs:{6}, SdkVersion:{7}",
                traceId,
                url,
                method,
                sourceId,
                serializedContent?.Length ?? 0,
                ServicePointManager.DnsRefreshTimeout,
                client.Timeout.TotalMilliseconds,
                AssemblyVersion);

            string responseString = null;
            var requestLatency = Stopwatch.StartNew();
            var stage = "SendRequest";
            var handlingServer = "Unknown";
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request).ConfigureAwait(false);
                Logger.Log(
                    LoggerLevel.Info,
                    LogId,
                    LogTag,
                    "Sent HTTP request, reading response. TraceId:{0}, Url:{1}, SendLatencyMs:{2}",
                    traceId,
                    url,
                    requestLatency.ElapsedMilliseconds);

                stage = "ReadResponse";

                IEnumerable<string> handlingServerValues;
                response.Headers.TryGetValues("__HandlingServerId__", out handlingServerValues);
                if (handlingServerValues != null)
                {
                    handlingServer = handlingServerValues.First();
                }

                requestLatency.Restart();

                if (response.Content.Headers.ContentType?.MediaType != null
                    && response.Content.Headers.ContentType.MediaType.Equals(SerializationConstants.OctetStreamContentType, StringComparison.OrdinalIgnoreCase))
                {
                    responseString = SerializationConstants.OctetStreamContentType;
                }
                else
                {
                    responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }

                Logger.Log(
                    LoggerLevel.Info,
                    LogId,
                    LogTag,
                    "Received HTTP response. TraceId:{0}, Url:{1}, HandlingServer:{2}, ReadLatencyMs:{3}, ResponseStatus:{4}, ResponseLength:{5}",
                    traceId,
                    url,
                    handlingServer,
                    requestLatency.ElapsedMilliseconds,
                    response.StatusCode,
                    responseString?.Length ?? 0);

                stage = "ValidateStatus";
                response.EnsureSuccessStatusCode();

                return Tuple.Create(responseString, response);
            }
            catch (Exception e)
            {
                var message = HandleServerSideThrottling(response, url, method, monitoringAccount) ?
                    $"HTTP request throttled by server. Url:{url}, Method:{method}, Response:{responseString ?? "<none>"}" :
                    $"Failed to get a response from the server. TraceId:{traceId.Value.ToString("B")}, Url:{request.RequestUri}, HandlingServer:{handlingServer} Stage:{stage}, "
                    +
                    $"LatencyMs:{requestLatency.ElapsedMilliseconds}, ResponseStatus:{response?.StatusCode.ToString() ?? "<none>"}, Response:{responseString}.";

                Logger.Log(LoggerLevel.Error, LogId, LogTag, message);

                throw new MetricsClientException(message, e, traceId.Value, response?.StatusCode);
            }
            finally
            {
                requestLatency.Stop();
            }
        }

        private static bool HandleServerSideThrottling(
            HttpResponseMessage response,
            Uri url,
            HttpMethod method,
            string monitoringAccount)
        {
            if (response != null &&
                (int)response.StatusCode == HttpStatusCodeThrottled &&
                response.Headers != null)
            {
                if (!response.Headers.Contains(ThrottledIdentityKey) || !response.Headers.Contains(RetryAfterKey))
                {
                    return false;
                }

                string throttledIdentity = response.Headers.GetValues(ThrottledIdentityKey).FirstOrDefault();
                if (string.IsNullOrEmpty(throttledIdentity))
                {
                    return false;
                }

                string retryAfter = response.Headers.GetValues(RetryAfterKey).FirstOrDefault();
                if (string.IsNullOrEmpty(retryAfter))
                {
                    return false;
                }

                int retyAfterSeconds;

                if (int.TryParse(retryAfter, out retyAfterSeconds))
                {
                    DateTime now = DateTime.UtcNow;

                    ServerThrottledIdentities.AddOrUpdate(
                        monitoringAccount,
                        key =>
                        {
                            var value = new ConcurrentDictionary<string, DateTime>();

                            value.TryAdd(throttledIdentity, now.AddSeconds(retyAfterSeconds));

                            return value;
                        },
                        (key, value) =>
                        {
                            value.AddOrUpdate(
                                throttledIdentity,
                                keyInner => now.AddSeconds(retyAfterSeconds),
                                (keyInner, valueInner) => now.AddSeconds(retyAfterSeconds));

                            return value;
                        });

                    return true;
                }

                Logger.Log(
                    LoggerLevel.Debug,
                    LogId,
                    "HandleServerSideThrottling",
                    "HTTP request throttled by server, but we could not parse retry-after header Url:{0}, Method:{1}, Retry-After {2}",
                    url,
                    method);
            }

            return false;
        }

        private static bool IsThrottledByServer(string monitoringAccount, string operation, HttpMethod httpMethod)
        {
            if (string.IsNullOrEmpty(monitoringAccount))
            {
                return false;
            }

            ConcurrentDictionary<string, DateTime> throttingIdentityToTimeToProceed;

            if (!ServerThrottledIdentities.TryGetValue(monitoringAccount, out throttingIdentityToTimeToProceed))
            {
                return false;
            }

            DateTime timeToProceedByOperation;
            throttingIdentityToTimeToProceed.TryGetValue(operation, out timeToProceedByOperation);

            DateTime timeToProceedByHttpMethod;
            throttingIdentityToTimeToProceed.TryGetValue(httpMethod.ToString(), out timeToProceedByHttpMethod);

            DateTime timeToProceed = new DateTime(Math.Max(timeToProceedByOperation.Ticks, timeToProceedByHttpMethod.Ticks));

            if (timeToProceed <= DateTime.UtcNow)
            {
                ConcurrentDictionary<string, DateTime> dummy;
                ServerThrottledIdentities.TryRemove(monitoringAccount, out dummy);
                return false;
            }

            return true;
        }

        private static void HandleGeneralClientThrottling(
            Uri url,
            HttpMethod method,
            Guid traceId)
        {
            var minute = DateTime.UtcNow.Minute;

            if (minute == currentMinute)
            {
                Interlocked.Increment(ref requestsSentInCurrentMinute);
            }
            else
            {
                currentMinute = minute;
                Interlocked.Exchange(ref requestsSentInCurrentMinute, 0);
            }

            var effectiveMaxWebRequestsPerMinute = Math.Max(DefaultMaxWebRequestsPerMinute, (int)MaxWebRequestsPerMinute);

            if (requestsSentInCurrentMinute > effectiveMaxWebRequestsPerMinute)
            {
                Logger.Log(
                    LoggerLevel.Debug,
                    LogId,
                    "HandleGeneralClientThrottling",
                    "HTTP request throttled. Url:{0}, Method:{2}, CurrentRequestsInMinute:{3}, AllowedRequestsInMinute:{4}",
                    url,
                    method,
                    requestsSentInCurrentMinute,
                    effectiveMaxWebRequestsPerMinute);

                throw new MetricsClientException(
                    $"Client size throttling: no more than [{effectiveMaxWebRequestsPerMinute}] requests can be issued in a minute.",
                    null,
                    traceId,
                    (HttpStatusCode)HttpStatusCodeThrottled);
            }
        }
    }
}
