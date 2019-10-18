//-------------------------------------------------------------------------------------------------
// <copyright file="IExpression.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// An expression that can be evaluated for computed sampling types or composite metrics.
    /// </summary>
    public interface IExpression
    {
        /// <summary>
        /// Gets or sets the name of the expression.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the expression.
        /// </summary>
        string Expression { get; set; }
    }
}
