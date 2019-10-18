// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentPurpose.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Metrics.Services.Common.BlobSegment
{
    /// <summary>
    /// The purpose of a segment - used for metric reporting to better understand the memory usage patterns.
    /// Also for debug mode it is possible to store the segment purpose within the segment and check
    /// if wrong segment is released during release operations.
    /// The values MUST start with 0 and increment by 1 (this assumption is made by BlobSegmentPool.PoolStatistics).
    /// the value of BlobSegment.ReleasedSegment cannot be used as SegmentPurpose value
    /// </summary>
    public enum SegmentPurpose
    {
        /// <summary>
        /// The segment purpose is undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// The segment is created to hold the incoming histogram.
        /// </summary>
        IncomingHistogram = 1,

        /// <summary>
        /// The segment is created to hold the merged histogram arena.
        /// </summary>
        MergedHistogramArena = 2,

        /// <summary>
        /// The segment holds the histogram in CST storage.
        /// </summary>
        HistogramForStorage = 3,

        /// <summary>
        /// The segment is created during the blob growth during write operation.
        /// </summary>
        GrowOnWritePastTheEndOfBlob = 4,

        /// <summary>
        /// The segment that is part of a clone of existing blob (request for data from storage that returns the copy of the data)
        /// </summary>
        BlobClone = 5,

        /// <summary>
        /// The segment that was read from disk file that contains histograms
        /// </summary>
        DiskHistogram = 6,

        /// <summary>
        /// The segment that was read from Azure Table Storage as a result of the query
        /// </summary>
        AzureTableStorageQueryHistogram = 7,

        /// <summary>
        /// The segment that was created for uploading data to Azure Table Storage.
        /// </summary>
        AzureTableStorageUploadHistogram = 8,

        /// <summary>
        /// The segment that was created during histograms aggregation on FE or FTA.
        /// </summary>
        AggregatedHistogram = 9,

        /// <summary>
        /// The segment that was created during the query-time histogram merge.
        /// </summary>
        QueryTimeMergeHistogram = 10,

        /// <summary>
        /// The segment that was created during list of buckets conversion.
        /// </summary>
        ListToBlobConversion = 11,

        /// <summary>
        /// The segment used to read encoded rollup data and then decode it when serving a query from rollups.
        /// </summary>
        RollupDataDecoding = 12,
    }
}
