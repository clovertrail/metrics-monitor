// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticInfo.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    /// <summary>
    /// A class hosting all diagnostic info for customers to send us to help troubleshooting.
    /// </summary>
    public sealed class DiagnosticInfo
    {
        /// <summary>
        /// Gets the trace ID.
        /// </summary>
        public string TraceId { get; internal set; }

        /// <summary>
        /// Gets the handling server identifier.
        /// </summary>
        public string HandlingServerId { get; internal set; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; internal set; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"TraceId:{this.TraceId}, HandlingServerId:{this.HandlingServerId}, ErrorMessage:{this.ErrorMessage}.";
        }
    }
}
