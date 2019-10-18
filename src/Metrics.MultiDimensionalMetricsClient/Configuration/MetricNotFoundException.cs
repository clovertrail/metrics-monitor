//-----------------------------------------------------------------------
// <copyright file="MetricNotFoundException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;

    /// <summary>
    /// Exception thrown when account object cannot be found.
    /// </summary>
    public sealed class MetricNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricNotFoundException"/> class.
        /// </summary>
        /// <param name="message">Message describing exception situation.</param>
        /// <param name="innerException">Inner exception which caused exception situation.</param>
        public MetricNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
