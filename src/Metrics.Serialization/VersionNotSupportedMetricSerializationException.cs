// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VersionNotSupportedMetricSerializationException.cs" company="Microsoft Corporation">
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
    /// An exception used to report about situation when version of deserializer doesn't support format of package it is used to deserialize.
    /// </summary>
    [Serializable]
    public sealed class VersionNotSupportedMetricSerializationException : MetricSerializationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionNotSupportedMetricSerializationException"/> class.
        /// </summary>
        /// <param name="message">A message explaining the cause for exception situation.</param>
        public VersionNotSupportedMetricSerializationException(string message)
            : base(message, null)
        {
        }
    }
}
