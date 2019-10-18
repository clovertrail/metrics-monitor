//-------------------------------------------------------------------------------------------------
// <copyright file="ComputedSamplingTypeExpression.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Computed sampling type expression.
    /// </summary>
    public sealed class ComputedSamplingTypeExpression : IComputedSamplingTypeExpression
    {
        private string name;
        private string expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputedSamplingTypeExpression"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="unit">The unit.</param>
        public ComputedSamplingTypeExpression(string name, string expression, string unit)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentNullException(nameof(expression));
            }

            this.Name = name;
            this.Expression = expression;
            this.IsBuiltIn = false;
            this.Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputedSamplingTypeExpression"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="isBuiltIn">if set to <c>true</c> [is built in].</param>
        /// <param name="unit">The unit.</param>
        [JsonConstructor]
        internal ComputedSamplingTypeExpression(string name, string expression, bool isBuiltIn, string unit)
        {
            this.name = name;
            this.expression = expression;
            this.IsBuiltIn = isBuiltIn;
            this.Unit = unit;
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

        /// <summary>
        /// Gets a value indicating whether this instance is built in.
        /// </summary>
        public bool IsBuiltIn { get; internal set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        public string Unit { get; set; }
    }
}
