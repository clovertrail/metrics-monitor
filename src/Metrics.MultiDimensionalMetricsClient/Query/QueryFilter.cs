// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueryFilter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Query
{
    /// <summary>
    /// Operator used to compare with the <see cref="QueryFilter.Operand"/>.
    /// </summary>
    public enum Operator
    {
        /// <summary>
        /// Operator was not specified, query is not considered valid.
        /// </summary>
        Undefined,

        /// <summary>
        /// Operator equal.
        /// </summary>
        Equal,

        /// <summary>
        /// Operator not equal.
        /// </summary>
        NotEqual,

        /// <summary>
        /// Operator greater than.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Operator less than.
        /// </summary>
        LessThan,

        /// <summary>
        /// Operator less than or equal.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Operator greater than or equal.
        /// </summary>
        GreaterThanOrEqual
    }

    /// <summary>
    /// Type that represents the filter used in the query.
    /// </summary>
    public sealed class QueryFilter
    {
        /// <summary>
        /// Filter object representing that no filtering should be done.
        /// </summary>
        public static readonly QueryFilter NoFilter = new QueryFilter(Operator.Undefined, 0.0);

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFilter"/> class.
        /// Create a filter to be used in a filtered dimension query.
        /// </summary>
        /// <param name="operator">The operator.</param>
        /// <param name="operand">The operand.</param>
        public QueryFilter(Operator @operator, double operand)
        {
            this.Operator = @operator;
            this.Operand = operand;
        }

        /// <summary>
        /// Operator to use when comparing time series aggregate to <see cref="Operand"/>.
        /// </summary>
        public Operator Operator { get; private set; }

        /// <summary>
        /// The value to compare to.
        /// </summary>
        public double Operand { get; private set; }

        /// <summary>
        /// Returns a string representing the current values of the instance, helpful for debugging and logging.
        /// </summary>
        /// <returns>
        /// A string representing the current values of the instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} {1}", this.Operator, this.Operand);
        }

        /// <summary>
        /// Determine if the provided object is equal to this <see cref="QueryFilter"/>.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The result of the equality test.</returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as QueryFilter);
        }

        /// <summary>
        /// Get hash code for this object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return this.Operator.GetHashCode() ^ this.Operand.GetHashCode();
        }

        /// <summary>
        /// Compare the provided <see cref="QueryFilter"/> to this one.
        /// </summary>
        /// <param name="otherFilter">The other query filter.</param>
        /// <returns>The result of the equality test.</returns>
        private bool Equals(QueryFilter otherFilter)
        {
            if (otherFilter == null)
            {
                return false;
            }

            return this.Operator.Equals(otherFilter.Operator)
                   && this.Operand == otherFilter.Operand;
        }
    }
}