// <copyright file="StampLocatorActivity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Cloud.Metrics.Client.ThirdParty
{
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
}
