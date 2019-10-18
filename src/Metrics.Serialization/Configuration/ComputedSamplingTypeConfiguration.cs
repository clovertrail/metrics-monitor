//-------------------------------------------------------------------------------------------------
// <copyright file="ComputedSamplingTypeConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.Configuration
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the configuration of computed sampling type.
    /// </summary>
    public class ComputedSamplingTypeConfiguration
    {
        /// <summary>
        /// This is the expression used by the build-in "Average" sampling type.
        /// </summary>
        public const string AverageExpression = @"raw.Sum / (raw.Count || 1)";

        /// <summary>
        /// This is the expression used by the build-in "NullableAverage" sampling type.
        /// </summary>
        public const string NullableAverageExpression = @"raw.Count ? (raw.Sum / raw.Count) : null";

        /// <summary>
        /// This is the expression used by the build-in "Rate" sampling type.
        /// </summary>
        public const string RateExpression = @"raw.Sum / 60";

        /// <summary>
        /// This is the expression used by the build-in "Standard deviation" sampling type.
        /// </summary>
        public const string StandardDeviationExpression = @"raw.Count < 2 ? null : Math.sqrt(raw.SumOfSquareDiffFromMean / raw.Count)";

        /// <summary>
        /// Gets or sets the friendly name for the computed metric, e.g.: Successful calls, CPU utilization, etc.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript expression used to produce this metric from raw values.
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Gets or sets the engine to be used to evaluate the expression. If it is null, empty or blank
        /// the engine defaults to the one configured for the appliucation.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ExpressionEngine { get; set; }

        /// <summary>
        /// Gets or sets the number suffix.
        /// </summary>
        /// <remarks>
        /// This is the unit to show in UI for the data points, such as seconds, ms, %, or any free text that make sense.
        /// </remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Unit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sampling type is added by the system.
        /// </summary>
        /// <remarks>
        /// These sampling type configurations cannot be deleted or modified.
        /// </remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsBuiltIn { get; set; }
    }
}
