// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SamplingTypes.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;

    /// <summary>
    /// Lists sampling types which metric may contain.
    /// </summary>
    [Flags]
    public enum SamplingTypes
    {
        None = 0x0,
        Min = 0x1,
        Max = 0x2,
        Sum = 0x4,
        Count = 0x10,
        Histogram = 0x20,
        SumCount = Sum | Count,
        SumCountMinMax = SumCount | Min | Max,
        HyperLogLogSketch = 0x40,
        DisableTrim = 0x80, // Not a sampling type per se but useful to send the information without changing serialization versions
        ClientSideLastSampleOnly = 0x100,
        DoubleValueType = 0x200, // Not a sampling type per se but useful to send the information without changing serialization versions
        DoubleValueStoredAsLongType = 0x400, // Not a sampling type per se but useful to send the information without changing serialization versions
        AggregatedTimeSeries = 0x800, // A flag indicating that samples belong to the aggregated time series (used in mStore aggregation pipeline)
        SendToCacheServer = 0x1000, // A flag indicating that samples aggregated on the FrontEnd should be sent to CacheServer
        SendToMStore = 0x2000, // A flag indicating that samples aggregated on the FrontEnd should be sent to mStore
        ComplexTypes = Histogram | HyperLogLogSketch,
        SumOfSquareDiffFromMean = 0x4000, // Used for calculating standard deviation, for more info look article https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Welford's_Online_algorithm
        TDigest = 0x8000,
    }
}
