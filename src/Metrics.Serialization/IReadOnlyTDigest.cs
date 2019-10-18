// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IReadOnlyTDigest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System.IO;

    /// <summary>
    /// Read-only tDigest interface
    /// </summary>
    public interface IReadOnlyTDigest
    {
        /// <summary>
        /// Serialize a tdigest to a given writer
        /// </summary>
        /// <param name="writer">The writer to use</param>
        void Serialize(BinaryWriter writer);
    }
}
