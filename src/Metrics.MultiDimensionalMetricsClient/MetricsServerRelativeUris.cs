// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetricsServerRelativeUris.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client
{
    /// <summary>
    /// The metrics server relative URIs.
    /// </summary>
    public static class MetricsServerRelativeUris
    {
        /// <summary>
        /// The relative URL for metrics data
        /// </summary>
        public const string DataRelativeUrl = "v1/data/metrics";

        /// <summary>
        /// The relative URL for metrics meta-data a.k.a. hinting data
        /// </summary>
        public const string MetaDataRelativeUrl = "v1/hint";

        /// <summary>
        /// The relative URL for metrics meta-data a.k.a. hinting data V2.
        /// </summary>
        public const string MetaDataRelativeUrlV2 = "v2/hint";

        /// <summary>
        /// The relative URL for metrics configuration
        /// </summary>
        public const string ConfigRelativeUrl = "v1/config/metrics";

        /// <summary>
        /// The V2 relative URL for metrics configuration
        /// </summary>
        public const string ConfigRelativeUrlV2 = "v2/config/metrics";

        /// <summary>
        /// The relative URL for account configuration
        /// </summary>
        public const string TenantConfigRelativeUrl = "v1/config";

        /// <summary>
        /// The relative URL for health configuration.
        /// </summary>
        public const string HealthConfigRelativeUrl = "v2/config/health";

        /// <summary>
        /// The relative URL for monitoring account configuration
        /// </summary>
        public const string AccountConfigRelativeUrl = "v1/config/tenant";

        /// <summary>
        /// The relative URL for health controller
        /// </summary>
        public const string HealthRelativeUrl = "v3/data/health";

        /// <summary>
        /// The relative Url for distributed query.
        /// </summary>
        public const string DistributedQueryRelativeUrl = "flight/dq/batchedReadv3";

        /// <summary>
        /// The query service relative URL.
        /// </summary>
        public const string QueryServiceRelativeUrl = "query";
    }
}