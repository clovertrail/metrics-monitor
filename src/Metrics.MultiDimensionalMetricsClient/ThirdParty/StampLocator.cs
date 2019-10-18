// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StampLocator.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.ThirdParty
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Cloud.Metrics.Client.Utility;

    using Newtonsoft.Json;

    /// <summary>
    /// Helper class to locate the stamp endpoint for Azure external customers' 3rd party accounts.
    /// </summary>
    public sealed class StampLocator : IStampLocator
    {
        private const string StampHostNameSuffix = ".metrics.nsatc.net";
        private const string FileNameForRegionStampMap = "RegionToGenevaMetricsStampMap.json";
        private static readonly Uri ThirdPartyRegionStampMapUrl = new Uri("https://stamplocator.metrics.nsatc.net/public/thirdPartyRegionStampMap");
        private readonly Uri stampLocatorUrl;
        private readonly string fullFilePathForRegionStampMap;
        private readonly HttpClient httpClient;
        private readonly TimeSpan refreshInternal = TimeSpan.FromHours(1);
        private readonly ActivityReporter activityReporter;
        private Timer timerToRefreshRegionStampMap;
        private Dictionary<string, string> regionStampMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="StampLocator" /> class.
        /// </summary>
        /// <param name="fullFilePathForRegionStampMap">The full file path for region stamp map.</param>
        /// <param name="regionStampMap">The region stamp map.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="stampLocatorUrl">The stamp locator URL to retrieve the region stamp map.</param>
        /// <param name="activityReporter">The activity reporter.</param>
        /// <exception cref="MetricsClientException">The region stamp map failed to initialize or is empty.</exception>
        private StampLocator(string fullFilePathForRegionStampMap, Dictionary<string, string> regionStampMap, HttpClient httpClient, Uri stampLocatorUrl, ActivityReporter activityReporter)
        {
            this.fullFilePathForRegionStampMap = fullFilePathForRegionStampMap;
            this.regionStampMap = regionStampMap;
            this.httpClient = httpClient;
            this.stampLocatorUrl = stampLocatorUrl;
            this.activityReporter = activityReporter;
        }

        /// <summary>
        /// The call back to report StampLocator activities. Consumers are expected to emit metrics and logs.
        /// </summary>
        /// <param name="activity">The activity.</param>
        /// <param name="isError">if set to <c>true</c>, the activity is an error.</param>
        /// <param name="detail">The detail about the activity.</param>
        public delegate void ActivityReporter(StampLocatorActivity activity, bool isError, string detail);

        /// <summary>
        /// Creates an instance of <see cref="StampLocator"/> asynchronously.
        /// </summary>
        /// <param name="folderToCacheRegionStampMap">The folder to cache region stamp map.</param>
        /// <param name="activityReporter">The activity reporter. The string argument contains the error detail when the activity results in an error; otherwise it is null.</param>
        /// <returns>An instance of <see cref="StampLocator"/>.</returns>
        public static Task<IStampLocator> CreateInstanceAsync(string folderToCacheRegionStampMap, ActivityReporter activityReporter)
        {
            if (string.IsNullOrWhiteSpace(folderToCacheRegionStampMap))
            {
                throw new ArgumentException("The argument is null or empty", nameof(folderToCacheRegionStampMap));
            }

            if (activityReporter == null)
            {
                throw new ArgumentNullException(nameof(activityReporter));
            }

            return CreateInstanceAsync(
                folderToCacheRegionStampMap,
                HttpClientHelper.CreateHttpClient(ConnectionInfo.DefaultTimeout),
                ThirdPartyRegionStampMapUrl,
                activityReporter);
        }

        /// <summary>
        /// Gets the stamp endpoint.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="azureRegion">The azure region.</param>
        /// <returns>The stamp endpoint for the given account identified by <paramref name="subscriptionId"/>.</returns>
        public Uri GetStampEndpoint(string subscriptionId, string azureRegion)
        {
            var stampName = this.GetStampName(subscriptionId, azureRegion);
            return new Uri($"https://{stampName}{StampHostNameSuffix}");
        }

        /// <summary>
        /// Gets the stamp name.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="azureRegion">The azure region.</param>
        /// <returns>The stamp name for the given account identified by <paramref name="subscriptionId"/>.</returns>
        public string GetStampName(string subscriptionId, string azureRegion)
        {
            if (this.regionStampMap.ContainsKey(azureRegion))
            {
                return this.regionStampMap[azureRegion];
            }

            throw new MetricsClientException($"There is no MDM stamp for region [{azureRegion}]. Available regions are [{string.Join(",", this.regionStampMap.Keys)}].");
        }

        /// <summary>
        /// Creates the instance asynchronously.
        /// </summary>
        /// <param name="folderToCacheRegionStampMap">The folder to cache region stamp map.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="stampLocatorUrl">The stamp locator URL.</param>
        /// <param name="activityReporter">The activity reporter.</param>
        /// <returns>
        /// An instance of <see cref="StampLocator" />.
        /// </returns>
        internal static async Task<IStampLocator> CreateInstanceAsync(
            string folderToCacheRegionStampMap,
            HttpClient httpClient,
            Uri stampLocatorUrl,
            ActivityReporter activityReporter)
        {
            string fullFilePathForRegionStampMap = null;
            Dictionary<string, string> regionStampMap = null;
            if (!string.IsNullOrWhiteSpace(folderToCacheRegionStampMap) && Directory.Exists(folderToCacheRegionStampMap))
            {
                fullFilePathForRegionStampMap = Path.Combine(folderToCacheRegionStampMap, FileNameForRegionStampMap);
                if (File.Exists(fullFilePathForRegionStampMap))
                {
                    string fileContent = null;
                    activityReporter(StampLocatorActivity.StartToLoadRegionStampMapFromLocalFile, false, $"File name:{fullFilePathForRegionStampMap}.");
                    try
                    {
                        fileContent = File.ReadAllText(fullFilePathForRegionStampMap);
                        regionStampMap = CreateNewRegionStampMap(fileContent);
                        activityReporter(StampLocatorActivity.FinishedLoadingRegionStampMapFromLocalFile, false, $"fileContent:{fileContent}, regionStampMap:{JsonConvert.SerializeObject(regionStampMap)}.");
                    }
                    catch (Exception e)
                    {
                        var errorMessage =
                            $"Failed to create the region stamp map from local file. FilePath:{fullFilePathForRegionStampMap}, FileContent:{fileContent}, Exception:{e}.";
                        activityReporter(StampLocatorActivity.FailedToLoadRegionStampMapFromLocalFile, true, errorMessage);
                    }
                }
                else
                {
                    activityReporter(StampLocatorActivity.FailedToLoadRegionStampMapFromLocalFile, true, $"File {fullFilePathForRegionStampMap} doesn't exist.");
                }
            }
            else
            {
                activityReporter(StampLocatorActivity.FailedToLoadRegionStampMapFromLocalFile, true, $"Folder {folderToCacheRegionStampMap} doesn't exist.");
            }

            var instance = new StampLocator(fullFilePathForRegionStampMap, regionStampMap, httpClient, stampLocatorUrl, activityReporter);

            await instance.RefreshNoThrow().ConfigureAwait(false);

            if (instance.regionStampMap == null || instance.regionStampMap.Count == 0)
            {
                throw new MetricsClientException("The region stamp map failed to initialize or is empty.");
            }

            instance.timerToRefreshRegionStampMap = new Timer(async state => await instance.RefreshNoThrow().ConfigureAwait(false), null, instance.refreshInternal, instance.refreshInternal);

            return instance;
        }

        private static Dictionary<string, string> CreateNewRegionStampMap(string response)
        {
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            var newRegionStampMap = new Dictionary<string, string>(2 * dictionary.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in dictionary)
            {
                newRegionStampMap.Add(kvp.Key, kvp.Value);
                newRegionStampMap.Add(kvp.Key.Replace(" ", string.Empty), kvp.Value);
            }

            return newRegionStampMap;
        }

        private async Task<bool> RefreshNoThrow()
        {
            this.activityReporter(StampLocatorActivity.StartToRefrehRegionStampMap, false, $"Url:{this.stampLocatorUrl}.");
            string response = null;
            try
            {
                var responseMessage = await HttpClientHelper.GetResponse(this.stampLocatorUrl, HttpMethod.Get, this.httpClient, "MA", "RefreshMap").ConfigureAwait(false);
                response = responseMessage.Item1;

                this.regionStampMap = CreateNewRegionStampMap(response);

                this.activityReporter(StampLocatorActivity.FinishedRefreshingRegionStampMap, false, $"regionStampMap:{JsonConvert.SerializeObject(this.regionStampMap)}.");
            }
            catch (Exception e)
            {
                string errorMessage = $"Failed to refresh the region stamp map. Response:{response}, Url:{this.stampLocatorUrl}, Exception:{e}.";
                this.activityReporter(StampLocatorActivity.FailedToRefrehRegionStampMap, true, errorMessage);
            }

            if (this.fullFilePathForRegionStampMap != null && response != null)
            {
                try
                {
                    this.activityReporter(StampLocatorActivity.StartToWriteRegionStampMapToLocalFile, false, $"File name:{this.fullFilePathForRegionStampMap}.");
                    File.WriteAllText(this.fullFilePathForRegionStampMap, response);
                    this.activityReporter(StampLocatorActivity.FinishedWritingRegionStampMapToLocalFile, false, string.Empty);
                }
                catch (Exception e)
                {
                    string errorMessage = $"Writing to {this.fullFilePathForRegionStampMap} failed with {e}.";
                    this.activityReporter(StampLocatorActivity.FailedToWriteRegionStampMapToLocalFile, true, errorMessage);
                }
            }

            return true;
        }
    }
}
