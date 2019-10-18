// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalAggregatedMetric.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;

    using Etw;
    using Online.Metrics.Serialization;

    /// <summary>
    /// The class representing the locally aggregated metric in the ETW stream.
    /// </summary>
    internal sealed class LocalAggregatedMetric : ILocalAggregatedMetric
    {
        /// <summary>
        /// Represents the character used to separate items within a list stored in a single ETW field.
        /// </summary>
        private static readonly char[] EtwListSeparatorChar = { '^' };

        /// <summary>
        /// The dimension name and dimension value pairs.
        /// </summary>
        private readonly Dictionary<string, string> dimensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the Monitoring Account to which this metric is reported.
        /// </summary>
        public string MonitoringAccount { get; private set; }

        /// <summary>
        /// Gets the metric namespace.
        /// </summary>
        public string MetricNamespace { get; private set; }

        /// <summary>
        /// Gets the metric name.
        /// </summary>
        public string MetricName { get; private set; }

        /// <summary>
        /// Gets the time in UTC when metric was reported.
        /// </summary>
        public DateTime MetricTimeUtc { get; private set; }

        /// <summary>
        /// Gets the dimension name-value dictionary.
        /// </summary>
        /// <remarks>The dimension names are case insensitive.</remarks>
        public IReadOnlyDictionary<string, string> Dimensions
        {
            get
            {
                return this.dimensions;
            }
        }

        /// <summary>
        /// Gets the scaling factor applied to metric values.
        /// </summary>
        public float ScalingFactor { get; private set; }

        /// <summary>
        /// Gets the number of samples for which this metric is reported.
        /// </summary>
        public uint Count { get; private set; }

        /// <summary>
        /// Gets the scaled sum of sample values reported this metric.
        /// </summary>
        public float ScaledSum { get; private set; }

        /// <summary>
        /// Gets the scaled minimum value of samples reported this metric.
        /// </summary>
        public float ScaledMin { get; private set; }

        /// <summary>
        /// Gets the scaled maximum value of samples reported this metric.
        /// </summary>
        public float ScaledMax { get; private set; }

        /// <summary>
        /// Gets the sum of sample values reported this metric.
        /// </summary>
        public ulong Sum { get; private set; }

        /// <summary>
        /// Gets the minimum value of samples reported this metric.
        /// </summary>
        public ulong Min { get; private set; }

        /// <summary>
        /// Gets the maximum value of samples reported this metric.
        /// </summary>
        public ulong Max { get; private set; }

        /// <summary>
        /// Converts content of the ETW event published by ME to a <see cref="LocalAggregatedMetric"/>
        /// </summary>
        /// <param name="etwMetricData">Object containing information about metric data sample.</param>
        /// <returns>A MetricData object representing a locally aggregated metric.</returns>
        internal static unsafe LocalAggregatedMetric ConvertToMetricData(NativeMethods.EventRecord* etwMetricData)
        {
            var metricData = new LocalAggregatedMetric();

            IntPtr pointerInPayload = etwMetricData->UserData;
            metricData.MonitoringAccount = EtwPayloadManipulationUtils.ReadString(ref pointerInPayload);
            metricData.MetricNamespace = EtwPayloadManipulationUtils.ReadString(ref pointerInPayload);
            metricData.MetricName = EtwPayloadManipulationUtils.ReadString(ref pointerInPayload);

            long timestamp = *((long*)pointerInPayload);
            metricData.MetricTimeUtc = DateTime.FromFileTimeUtc(timestamp);
            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(long));

            // Read the dimension name and values and split them out.
            var dimensionNames = EtwPayloadManipulationUtils.ReadString(ref pointerInPayload);
            var dimensionValues = EtwPayloadManipulationUtils.ReadString(ref pointerInPayload);

            if (!string.IsNullOrWhiteSpace(dimensionNames) && !string.IsNullOrWhiteSpace(dimensionValues))
            {
                var splitDimensionNames = dimensionNames.Split(EtwListSeparatorChar, StringSplitOptions.None);
                var splitDimensionValues = dimensionValues.Split(EtwListSeparatorChar, StringSplitOptions.None);

                // Expected that both lengths be the same since they are written this way.
                for (var x = 0; x < splitDimensionNames.Length && x < splitDimensionValues.Length; ++x)
                {
                    metricData.dimensions[splitDimensionNames[x]] = splitDimensionValues[x];
                }
            }

            var scalingFactor = *((float*)pointerInPayload);
            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(float));
            metricData.ScalingFactor = scalingFactor;

            var samplingTypes = (SamplingTypes)(*((int*)pointerInPayload));
            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(int));

            if ((samplingTypes & SamplingTypes.Min) != 0)
            {
                metricData.Min = *((ulong*)pointerInPayload);
                metricData.ScaledMin = metricData.Min / scalingFactor;
            }

            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(ulong));

            if ((samplingTypes & SamplingTypes.Max) != 0)
            {
                metricData.Max = *((ulong*)pointerInPayload);
                metricData.ScaledMax = metricData.Max / scalingFactor;
            }

            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(ulong));

            if ((samplingTypes & SamplingTypes.Sum) != 0)
            {
                metricData.Sum = *((ulong*)pointerInPayload);
                metricData.ScaledSum = metricData.Sum / scalingFactor;
            }

            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(ulong));

            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(float));

            if ((samplingTypes & SamplingTypes.Count) != 0)
            {
                metricData.Count = *((uint*)pointerInPayload);
            }

            pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(uint));

            return metricData;
        }
    }
}