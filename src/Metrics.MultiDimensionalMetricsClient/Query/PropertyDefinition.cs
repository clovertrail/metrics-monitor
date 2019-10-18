// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyDefinition.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System;
    using Metrics;
    using Newtonsoft.Json;

    /// <summary>
    /// Aggregation types which can be used to create properties based on time series data
    /// </summary>
    public enum PropertyAggregationType
    {
        Average,
        Sum,
        Min,
        Max
    }

    /// <summary>
    /// Defines the calculation that will occur to get a query service property.  The sampling type to act
    /// on, and the aggregation to perform on that sampling type.
    /// </summary>
    public sealed class PropertyDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDefinition"/> class.
        /// </summary>
        /// <param name="propertyAggregationType">Type of the property aggregation.</param>
        /// <param name="samplingType">Name of the sampling type.</param>
        [JsonConstructor]
        public PropertyDefinition(PropertyAggregationType propertyAggregationType, SamplingType samplingType)
        {
            this.PropertyAggregationType = propertyAggregationType;
            this.SamplingType = samplingType;
            this.PropertyName = GetPropertyName(this.PropertyAggregationType, this.SamplingType.Name);
        }

        /// <summary>
        /// Gets the type of the property aggregation.
        /// </summary>
        public PropertyAggregationType PropertyAggregationType { get; }

        /// <summary>
        /// Gets the name of the sampling type.
        /// </summary>
        public SamplingType SamplingType { get; }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the name of the resulted property.
        /// </summary>
        /// <param name="propertyAggregationType">Property aggregation type.</param>
        /// <param name="samplingTypeName">Sampling type to which aggregation is applied.</param>
        /// <returns>Name of the property.</returns>
        public static string GetPropertyName(PropertyAggregationType propertyAggregationType, string samplingTypeName)
        {
            switch (propertyAggregationType)
            {
                case PropertyAggregationType.Average:
                    return $"TAVG({samplingTypeName})";
                case PropertyAggregationType.Sum:
                    return $"TSUM({samplingTypeName})";
                case PropertyAggregationType.Max:
                    return $"TMAX({samplingTypeName})";
                case PropertyAggregationType.Min:
                    return $"TMIN({samplingTypeName})";
                default:
                    throw new ArgumentException($"Unexpected propertyAggregationType: {propertyAggregationType}.", nameof(propertyAggregationType));
            }
        }
    }
}
