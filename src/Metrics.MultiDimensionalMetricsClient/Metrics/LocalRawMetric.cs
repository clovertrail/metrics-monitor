// -----------------------------------------------------------------------
// <copyright file="LocalRawMetric.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics
{
    using System;
    using System.Collections.Generic;

    using Etw;
    using Logging;

    /// <summary>
    /// A class representing data for one raw metric.
    /// </summary>
    internal sealed class LocalRawMetric : ILocalRawMetric
    {
        /// <summary>
        /// The platform metric etw operation code
        /// </summary>
        private const int PlatformMetricEtwOperationCode = 51;

        /// <summary>
        /// Custom log id to be used in the log statements.
        /// </summary>
        private static readonly object LogId = Logger.CreateCustomLogId("LocalRawMetric");

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
        /// Gets the metric dimensions.
        /// </summary>
        public IDictionary<string, string> Dimensions { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the metric is a platform metric.
        /// In such case its value should be taken using property <see cref="MetricDoubleValue"/>.
        /// </summary>
        public bool IsPlatformMetric { get; private set; }

        /// <summary>
        /// Gets the metric time bucket.
        /// </summary>
        public DateTime MetricTimeUtc { get; private set; }

        /// <summary>
        /// Gets the sample value of metric emitted using Metric API.
        /// </summary>
        public ulong MetricLongValue { get; private set; }

        /// <summary>
        /// Gets the sample value of the Platform specific metric.
        /// </summary>
        public double MetricDoubleValue { get; private set; }

        /// <summary>
        /// Converts content of the ETW event to <see cref="LocalRawMetric"/>.
        /// </summary>
        /// <param name="etwMetricData">Object containing information about metric data sample.</param>
        /// <returns>A <see cref="LocalRawMetric"/> object representing metric sample data.</returns>
        /// <exception cref="ArgumentException">Throw when information contained in metricDataRecord is in incorrect format.</exception>
        internal static unsafe LocalRawMetric ConvertToMetricData(NativeMethods.EventRecord* etwMetricData)
        {
            try
            {
                // Read ETW event time and use as metric time
                var etwTimeUtc = DateTime.FromFileTimeUtc(etwMetricData->EventHeader.TimeStamp);

                IntPtr pointerInPayload = etwMetricData->UserData;

                // Get number of dimensions
                ushort dimensionsCount = *((ushort*)pointerInPayload);
                pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(ushort));

                // Shift 6 bytes as this space is reserved for alignment
                pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(ushort) + sizeof(uint));

                // If time was reported with metric, use it
                long timestamp = *((long*)pointerInPayload);
                pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(long));

                var metricTimeUtc = timestamp == 0 ? etwTimeUtc : DateTime.FromFileTimeUtc(timestamp);

                // Metric sample value which is either value or delta
                double doubleMetricSampleValue = 0;
                long metricSampleValue = 0;
                if (etwMetricData->EventHeader.Id == PlatformMetricEtwOperationCode)
                {
                    doubleMetricSampleValue = *((double*)pointerInPayload);
                }
                else
                {
                    metricSampleValue = *((long*)pointerInPayload);
                }

                pointerInPayload = EtwPayloadManipulationUtils.Shift(pointerInPayload, sizeof(long));

                // Read monitoring account, metric namespace and name
                var monitoringAccount = EtwPayloadManipulationUtils.ReadString(ref pointerInPayload);
                var metricNameSpace = EtwPayloadManipulationUtils.ReadString(ref pointerInPayload);
                var metricName = EtwPayloadManipulationUtils.ReadString(ref pointerInPayload);

                var dimensionNames = new List<string>();
                for (int i = 0; i < dimensionsCount; ++i)
                {
                    dimensionNames.Add(EtwPayloadManipulationUtils.ReadString(ref pointerInPayload));
                }

                var dimensionValues = new List<string>();
                for (int i = 0; i < dimensionsCount; ++i)
                {
                    dimensionValues.Add(EtwPayloadManipulationUtils.ReadString(ref pointerInPayload));
                }

                var dimensions = new Dictionary<string, string>();
                for (int i = 0; i < dimensionsCount; ++i)
                {
                    dimensions[dimensionNames[i]] = dimensionValues[i];
                }

                return new LocalRawMetric
                {
                    IsPlatformMetric =
                        etwMetricData->EventHeader.Id == PlatformMetricEtwOperationCode,
                    MetricTimeUtc = metricTimeUtc,
                    MetricLongValue = (ulong)metricSampleValue,
                    MetricDoubleValue = doubleMetricSampleValue,
                    MonitoringAccount = monitoringAccount,
                    MetricNamespace = metricNameSpace,
                    MetricName = metricName,
                    Dimensions = dimensions
                };
            }
            catch (Exception e)
            {
                Logger.Log(
                    LoggerLevel.Error,
                    LogId,
                    "ConvertToMetricData",
                    "Failed to read raw metric daat from the ETW event payload.",
                    e);

                throw;
            }
        }
    }
}