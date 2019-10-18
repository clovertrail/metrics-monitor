// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrcCheckFailedSerializationException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <author email="selavrin">
//     Sergii Lavrinenko
// </author>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System;

    /// <summary>
    /// An exception used to report about situation when CRC check fails.
    /// </summary>
    [Serializable]
    public sealed class CrcCheckFailedSerializationException : MetricSerializationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrcCheckFailedSerializationException"/> class.
        /// </summary>
        /// <param name="message">A message explaining the cause for exception situation.</param>
        public CrcCheckFailedSerializationException(string message)
            : base(message, null)
        {
        }
    }
}
