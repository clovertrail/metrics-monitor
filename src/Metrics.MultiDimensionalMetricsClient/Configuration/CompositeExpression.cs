//-------------------------------------------------------------------------------------------------
// <copyright file="CompositeExpression.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;

    /// <summary>
    /// Expression to be evaluated to create sampling types for the composite metric.
    /// </summary>
    public sealed class CompositeExpression : IExpression
    {
        private string name;
        private string expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeExpression"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="expression">The expression.</param>
        public CompositeExpression(string name, string expression)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentNullException(nameof(expression));
            }

            this.name = name;
            this.expression = expression;
        }

        /// <summary>
        /// Gets or sets the name of the expression.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.name = value;
            }
        }

        /// <summary>
        /// Gets or sets the expression.
        /// </summary>
        public string Expression
        {
            get
            {
                return this.expression;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.expression = value;
            }
        }
    }
}
