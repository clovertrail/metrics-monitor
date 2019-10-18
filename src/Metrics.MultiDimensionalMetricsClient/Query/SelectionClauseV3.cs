// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectionClauseV3.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    /// <summary>
    /// Used to determine how many results should be returned from the query, how they should be ordered, and what criteria to used to determine
    /// which series are included.
    /// </summary>
    public sealed class SelectionClauseV3
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionClauseV3"/> class.
        /// </summary>
        /// <param name="propertyDefinition">The property used .</param>
        /// <param name="numberOfResultsToReturn">The number of results to return.</param>
        /// <param name="orderBy">The ordering of the selection.</param>
        public SelectionClauseV3(PropertyDefinition propertyDefinition, int numberOfResultsToReturn, OrderBy orderBy)
        {
            this.PropertyDefinition = propertyDefinition;
            this.NumberOfResultsToReturn = numberOfResultsToReturn;
            this.OrderBy = orderBy;
        }

        /// <summary>
        /// Defines which sampling type data is used to determine the top series.
        /// </summary>
        public PropertyDefinition PropertyDefinition { get; }

        /// <summary>
        /// Gets the number of time series to return.
        /// </summary>
        public int NumberOfResultsToReturn { get; }

        /// <summary>
        /// Gets the ordering of the selection
        /// </summary>
        public OrderBy OrderBy { get; }
    }
}
