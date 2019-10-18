// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectionClause.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    /// <summary>
    /// Ordering enumeration for Top N selection clause, default is descending.
    /// </summary>
    public enum OrderBy
    {
        /// <summary>
        /// Ordering not specified.  Selection clause is not considered valid.
        /// </summary>
        Undefined,

        /// <summary>
        /// Descending ordering for Top N selection clause, this is the default value.
        /// </summary>
        Descending,

        /// <summary>
        /// Ascending ordering for Top N selection clause.
        /// </summary>
        Ascending,
    }

    /// <summary>
    /// The type of selection to perform.
    /// </summary>
    public enum SelectionType
    {
        /// <summary>
        /// Selection type not specified.  Selection clause is not considered valid.
        /// </summary>
        Undefined,

        /// <summary>
        /// Top N should return the top N values that meet the filter criteria
        /// </summary>
        TopValues,

        /// <summary>
        /// Top N should return the top N percent of values that meet the filter criteria
        /// </summary>
        TopPercent,
    }

    /// <summary>
    /// This class represents the selection clause of the query
    /// </summary>
    public sealed class SelectionClause
    {
        /// <summary>
        /// Selection clause to indicate that all results should be returned.
        /// </summary>
        public static readonly SelectionClause AllResults = new SelectionClause(SelectionType.Undefined, 0, OrderBy.Undefined);

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionClause"/> class.
        /// </summary>
        /// <param name="selectionType">Type of the selection clause.</param>
        /// <param name="quantityToSelect">The quantity to select.</param>
        /// <param name="orderBy">The ordering of the selection.</param>
        public SelectionClause(SelectionType selectionType, int quantityToSelect, OrderBy orderBy)
        {
            this.SelectionType = selectionType;
            this.QuantityToSelect = quantityToSelect;
            this.OrderBy = orderBy;
        }

        /// <summary>
        /// Gets the type of the selection clause.
        /// </summary>
        public SelectionType SelectionType { get; private set; }

        /// <summary>
        /// Gets the quantity to select.
        /// </summary>
        public int QuantityToSelect { get; private set; }

        /// <summary>
        /// Gets the ordering of the selection
        /// </summary>
        public OrderBy OrderBy { get; private set; }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as SelectionClause);
        }

        /// <summary>
        /// Get hash code for this object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return this.SelectionType.GetHashCode() ^ this.QuantityToSelect.GetHashCode() ^ this.OrderBy.GetHashCode();
        }

        /// <summary>
        /// Compare this Selection Clause to the specified other clause.
        /// </summary>
        /// <param name="otherClause">The other clause.</param>
        /// <returns>Result of the equality test.</returns>
        private bool Equals(SelectionClause otherClause)
        {
            if (otherClause == null)
            {
                return false;
            }

            return this.SelectionType.Equals(otherClause.SelectionType)
                   && this.QuantityToSelect.Equals(otherClause.QuantityToSelect)
                   && this.OrderBy == otherClause.OrderBy;
        }
    }
}
