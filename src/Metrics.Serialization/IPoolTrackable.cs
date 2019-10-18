// <copyright file="IPoolTrackable.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Metrics.Services.Common
{
    /// <summary>
    /// The state of the poolable object
    /// </summary>
    public enum PoolObjectTrackingInfo : byte
    {
        /// <summary>
        /// The object does not support allocation/release tracking
        /// </summary>
        TrackingNotSupported = 0,

        /// <summary>
        /// The object is currently owned by the pool and was not allocated to the user code
        /// </summary>
        InPool = 1,

        /// <summary>
        /// The object is currently allocated from the pool and is owned by the user code
        /// </summary>
        Allocated = 2,

        /// <summary>
        /// The object was instantiated but the value was not set.
        /// TODO: this should have the value of 0 (and TrackingNotSupported should be 3) when unit tests that return the object to pool without
        /// allocating it from pool are fixed - this way we can catch the NotSet value during release for cases of objects being
        /// returned to pool without being allocated from the pool
        /// </summary>
        NotSet = 3,
    }

    /// <summary>
    /// All poolable objects must implement this interface to
    /// enable object tracking by pool code
    /// </summary>
    public interface IPoolTrackable
    {
        /// <summary>
        /// Gets or sets the value indicating whether this object is currently owned by a pool.
        /// This property is set by object pool code.
        /// Initial value of the property is PoolObjectTrackingInfo.TrackingNotSupported
        /// </summary>
        PoolObjectTrackingInfo PoolObjectTrackingInfo { get; set; }
    }
}