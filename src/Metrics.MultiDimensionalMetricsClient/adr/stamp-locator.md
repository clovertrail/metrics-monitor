# Stamp Locator

Date: 2/11/2019

## Status

## Context

Third party monitoring accounts use a different tenancy model, i.e., the same account exists on all azmon regional stamps.
Therefore, to query metrics, partner team(s) need to use our client API to determine the target regional stamp first for a given region.
There is a hard coded region to MDM stamp mapping in StampLocator of the client library, 
so partner team(s) need to take a new client library Nuget package when a new region support is added. 

## Decision

We will create a new *public* API in FE to return this mapping. To improve the reliability: 
1) We will have a new DNS name *stamplocator.metrics.nsatc.net* which includes 3 stamps in 3 stamp groups in 3 different continents. 
The 3 stamps are azglobal, azmonsuk, and azmonejp in priority/tier order. 
2) StampLocator.CreateInstanceAsync asks for a folder on the local disk to cache this mapping in case no MDM stamp is reachable. 
The folder can be provisioned as an Azure local resource so that it can survive machine reimaging. 
If no MDM stamp is reachable and no local cache is available, StampLocator.CreateInstanceAsync will throw; 
otherwise it will succeed and auto-refresh will happen in the background hourly.
We expose the following activities for partner team(s) to add monitoring and logging.


```csharp
    /// <summary>
    /// Creates an instance of <see cref="IStampLocator"/> asynchronously.
    /// </summary>
    /// <param name="folderToCacheRegionStampMap">The folder to cache region stamp map.</param>
    /// <param name="activityReporter">The activity reporter. The string argument contains the error detail when the activity results in an error; otherwise it is null.</param>
    /// <returns>An instance of <see cref="StampLocator"/>.</returns>
    public static Task<IStampLocator> CreateInstanceAsync(string folderToCacheRegionStampMap, Action<StampLocatorActivity, string> activityReporter)

    /// <summary>
    /// The stamp locator activities.
    /// </summary>
    public enum StampLocatorActivity
    {
        /// <summary>
        /// Refreshing the region stamp map from the MDM backend API.
        /// </summary>
        StartToRefrehRegionStampMap,
        FinishedRefreshingRegionStampMap,
        FailedToRefrehRegionStampMap,

        /// <summary>
        /// Loading the region stamp map from the local file regionStampMap.json.
        /// </summary>
        StartToLoadRegionStampMapFromLocalFile,
        FinishedLoadingRegionStampMapFromLocalFile,
        FailedToLoadRegionStampMapFromLocalFile,

        /// <summary>
        /// Writing the region stamp map to the local file regionStampMap.json.
        /// </summary>
        StartToWriteRegionStampMapToLocalFile,
        FinishedWritingRegionStampMapToLocalFile,
        FailedToWriteRegionStampMapToLocalFile,
    }

```