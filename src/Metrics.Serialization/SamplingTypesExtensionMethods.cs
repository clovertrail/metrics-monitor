// <copyright file="SamplingTypesExtensionMethods.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Online.Metrics.Serialization
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The helper class to hold the extension methods for <see cref="SamplingTypes"/> type.
    /// </summary>
    public static class SamplingTypesExtensionMethods
    {
        /// <summary>
        /// Returns true if the samplingTypes instance has value as one of the bits set.
        /// </summary>
        /// <param name="samplingTypes">The sampling types to check.</param>
        /// <param name="value">The value to look for.</param>
        /// <returns>True if value is present in samplingTypes, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Includes(this SamplingTypes samplingTypes, SamplingTypes value)
        {
            return (samplingTypes & value) == value;
        }
    }
}
