//-------------------------------------------------------------------------------------------------
// <copyright file="IComputedSamplingTypeExpression.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// An expression that can be evaluated for computed sampling types or composite metrics.
    /// </summary>
    public interface IComputedSamplingTypeExpression : IExpression
    {
        /// <summary>
        /// Gets a value indicating whether this instance is built in.
        /// </summary>
        bool IsBuiltIn { get; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        string Unit { get; set; }
    }
}
