// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueryResultQualityInfo.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Represents the query result quality information.
    /// </summary>
    /// <remarks>
    /// TODO: Add time based data loss support.
    /// </remarks>
    public sealed class QueryResultQualityInfo
    {
        private readonly ConcurrentDictionary<string, int> droppedTimeSeries = new ConcurrentDictionary<string, int>();
        private int totalDroppedTimeSeries;
        private int totalEstimatedTimeSeries;

        /// <summary>
        /// Gets or sets the total estimated time series for the query.
        /// </summary>
        public int TotalEstimatedTimeSeries
        {
            get { return this.totalEstimatedTimeSeries; }
            set { this.totalEstimatedTimeSeries = value; }
        }

        /// <summary>
        /// Gets or sets the total dropped time series for the query.
        /// </summary>
        public int TotalDroppedTimeSeries
        {
            get { return this.totalDroppedTimeSeries; }
        }

        /// <summary>
        /// Gets the total evaluated time series for the query.  This represents the number of raw series query
        /// service processed to obtain the customer-facing result.
        /// </summary>
        public int TotalEvaluatedTimeSeries
        {
            get { return this.TotalEstimatedTimeSeries - this.totalDroppedTimeSeries; }
        }

        /// <summary>
        /// Gets the dropped time series reasons.
        /// </summary>
        /// <remarks>
        /// Used for unit testing purposes.
        /// </remarks>
        /// <returns>Dropped time series reasons.</returns>
        public ICollection<string> GetDroppedTimeSeriesReasons()
        {
            return this.droppedTimeSeries.Keys;
        }

        /// <summary>
        /// Gets the dropped time series by reason.
        /// </summary>
        /// <param name="reason">The reason.</param>
        /// <remarks>
        /// Used for unit testing purposes.
        /// </remarks>
        /// <returns>Dropped time series by reason.</returns>
        public int GetDroppedTimeSeriesByReason(string reason)
        {
            return this.droppedTimeSeries[reason];
        }

        /// <summary>
        /// Register dropped time series during the query.
        /// </summary>
        /// <param name="reason">The reason for dropping the time series.</param>
        /// <param name="count">The count.</param>
        public void RegisterDroppedTimeSeries(string reason, int count)
        {
            if (count > 0)
            {
                Interlocked.Add(ref this.totalDroppedTimeSeries, count);
                this.droppedTimeSeries.AddOrUpdate(
                    reason,
                    count,
                    (key, existingValue) => existingValue + count);
            }
        }

        /// <summary>
        /// Registers the estimated time series during a query.  This is the
        /// number of time series we would ideally get back from the store.
        /// </summary>
        /// <param name="count">The count.</param>
        public void RegisterEstimatedTimeSeries(int count)
        {
            Interlocked.Add(ref this.totalEstimatedTimeSeries, count);
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string result = $"Total Estimated TimeSeries:{this.TotalEstimatedTimeSeries}, Total Dropped TimeSeries:{this.totalDroppedTimeSeries}.";
            if (this.totalDroppedTimeSeries > 0)
            {
                result = this.droppedTimeSeries.Aggregate(result, (current, dropReason) => current + $"{dropReason.Key}:{dropReason.Value}");
            }

            return result;
        }

        /// <summary>
        /// Deserializes query quality info from given stream reader.
        /// </summary>
        /// <param name="reader">The stream reader containing quality information.</param>
        public void Deserialize(BinaryReader reader)
        {
            // Any modifications here should be replicated to D:\onebranch\EngSys\MDA\MetricsAndHealth\src\DistributedQuery\Interfaces\QueryResultQuality.cs
            reader.ReadByte();
            this.TotalEstimatedTimeSeries = reader.ReadInt32();
            this.totalDroppedTimeSeries = reader.ReadInt32();
            if (this.totalDroppedTimeSeries > 0)
            {
                var numberOfReasons = reader.ReadInt32();
                for (int i = 0; i < numberOfReasons; i++)
                {
                    var reason = reader.ReadString();
                    var dropCount = reader.ReadInt32();
                    this.RegisterDroppedTimeSeries(reason, dropCount);
                }
            }
        }

        /// <summary>
        /// Serializes the quality info into given stream writer.
        /// </summary>
        /// <param name="writer">The stream writer.</param>
        public void Serialize(BinaryWriter writer)
        {
            // Any modifications here should be replicated to D:\onebranch\EngSys\MDA\MetricsAndHealth\src\DistributedQuery\Interfaces\QueryResultQuality.cs
            // Version
            writer.Write((byte)0);
            writer.Write(this.TotalEstimatedTimeSeries);
            writer.Write(this.totalDroppedTimeSeries);
            if (this.totalDroppedTimeSeries > 0)
            {
                writer.Write(this.droppedTimeSeries.Count);
                foreach (var dropReason in this.droppedTimeSeries)
                {
                    writer.Write(dropReason.Key);
                    writer.Write(dropReason.Value);
                }
            }
        }

        /// <summary>
        /// Aggregates the data from given qualty info.
        /// </summary>
        /// <param name="source">The source quality info.</param>
        public void Aggregate(QueryResultQualityInfo source)
        {
            if (source.totalDroppedTimeSeries > 0)
            {
                foreach (var reason in source.droppedTimeSeries)
                {
                    this.RegisterDroppedTimeSeries(reason.Key, reason.Value);
                }
            }
        }
    }
}
