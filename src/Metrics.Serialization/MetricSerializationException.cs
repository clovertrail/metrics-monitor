// <copyright file="MetricSerializationException.cs" company="Microsoft Corporation">
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
    /// A general exception used to report about metric serialization or deserialization failures.
    /// </summary>
    [Serializable]
    public class MetricSerializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricSerializationException"/> class.
        /// </summary>
        /// <param name="message">Message describing exception situation.</param>
        /// <param name="innerException">Inner exception which caused exception situation.</param>
        /// <param name="isInvalidData">True if the exception is because of invalid data in the packet.</param>
        public MetricSerializationException(string message, Exception innerException, bool isInvalidData = false)
            : base(message, innerException)
        {
            this.IsInvalidData = true;
        }

        /// <summary>
        /// Gets a value indicating whether the failure is because of invalid data in the packet.
        /// </summary>
        public bool IsInvalidData { get; }
    }
}
