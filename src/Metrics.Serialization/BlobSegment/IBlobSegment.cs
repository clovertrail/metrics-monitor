//-------------------------------------------------------------------------------------------------
// <copyright file="IBlobSegment.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Metrics.Services.Common.BlobSegment
{
    /// <summary>
    /// Blob segment interface
    /// </summary>
    public interface IBlobSegment : IPoolTrackable
    {
        /// <summary>
        /// Gets the data that is stored in the segment.
        /// </summary>
        byte[] Data { get; }

        /// <summary>
        /// Gets or sets the reference to the next segment in the blob (null for the last segment of the blob).
        /// </summary>
        IBlobSegment Next { get; set; }

        /// <summary>
        /// Initializes an instance of BlobSegment class.
        /// </summary>
        /// <param name="nextSegment">The next segment in the linked list.</param>
        /// <param name="purpose">Purpose of the segment usage. Useful for debugging memory leaks.</param>
        void Initialize(
            IBlobSegment nextSegment,
            SegmentPurpose purpose);
    }
}