// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilteredQueryResponseDeserializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Online.Metrics.Serialization;
    using Query;

    /// <summary>
    /// The class to deserialize the binary payload in response.
    /// </summary>
    public sealed class FilteredQueryResponseDeserializer
    {
        /// <summary>
        /// Deserializes the specified stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize.</param>
        /// <returns>
        /// A list of <see cref="FilteredTimeSeriesQueryResponse"/>.
        /// </returns>
        public static IReadOnlyList<IFilteredTimeSeriesQueryResponse> Deserialize(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var numOfResponses = (int)SerializationUtils.ReadUInt32FromBase128(reader);
                var results = new List<FilteredTimeSeriesQueryResponse>(numOfResponses);
                for (int i = 0; i < numOfResponses; i++)
                {
                    // Strip off a version added by query service host - QueryCoordinatorHost.cs.
                    var version = reader.ReadByte();

                    var numOfQueryResults = reader.ReadInt32();
                    for (int k = 0; k < numOfQueryResults; k++)
                    {
                        var filteredTimeSeriesQueryResponse = new FilteredTimeSeriesQueryResponse();
                        filteredTimeSeriesQueryResponse.Deserialize(reader);
                        results.Add(filteredTimeSeriesQueryResponse);
                    }
                }

                return results;
            }
        }
    }
}
