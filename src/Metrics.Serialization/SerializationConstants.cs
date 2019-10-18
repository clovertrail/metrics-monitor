// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    /// <summary>
    /// Serialization constants.
    /// </summary>
    public static class SerializationConstants
    {
        /// <summary>
        /// The default series resolution in minutes.
        /// </summary>
        public const int DefaultSeriesResolutionInMinutes = 1;

        /// <summary>
        /// The http header indicating if we should skip scaling the metrics data.
        /// </summary>
        public const string ScalingFactorDisabledHeader = "__ScalingFactorDisabled__";

        /// <summary>
        /// The MIME type for octet streams.
        /// </summary>
        public const string OctetStreamContentType = "application/octet-stream";

        /// <summary>
        /// The trace identifier header.
        /// </summary>
        public const string TraceIdHeader = "TraceGuid";

        /// <summary>
        /// The client identifier header.
        /// </summary>
        public const string ClientIdHeader = "ClientId";

        /// <summary>
        /// The maximum metric description length allowed.
        /// </summary>
        public const int MaximumMetricDescriptionLength = 1024;

        /// <summary>
        /// The maximum description length allowed.
        /// </summary>
        public const int MaximumDescriptionLength = 256;
    }
}
