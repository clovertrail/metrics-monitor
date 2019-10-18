//-----------------------------------------------------------------------
// <copyright file="ConfigurationValidationException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    using System;

    /// <summary>
    /// The type of validation being performed at the time of the failure.
    /// </summary>
    public enum ValidationType
    {
        ServerSide,
        DuplicateMetricSource,
        DuplicateDimension,
        DuplicatePreaggregate,
        DuplicateSamplingType,
        BuiltInTypeRemoved,
    }

    /// <summary>
    /// Exception thrown when account object cannot be found.
    /// </summary>
    public sealed class ConfigurationValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class.
        /// </summary>
        /// <param name="message">Message describing exception situation.</param>
        /// <param name="validationType">The type of the validation that failed.</param>
        public ConfigurationValidationException(string message, ValidationType validationType)
            : base(message)
        {
            this.ValidationType = validationType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class.
        /// </summary>
        /// <param name="message">Message describing exception situation.</param>
        /// <param name="validationType">The type of the validation that failed.</param>
        /// <param name="innerException">Inner exception which caused exception situation.</param>
        public ConfigurationValidationException(string message, ValidationType validationType, Exception innerException)
            : base(message, innerException)
        {
            this.ValidationType = validationType;
        }

        /// <summary>
        /// The type of the validation which failed.
        /// </summary>
        public ValidationType ValidationType { get; }
    }
}
