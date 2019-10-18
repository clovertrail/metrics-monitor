//-------------------------------------------------------------------------------------------------
// <copyright file="ClientAssemblyMigration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System;

    /// <summary>
    /// Represents a single conversion from the serialized assembly to the deserialization assembly.
    /// </summary>
    internal class ClientAssemblyMigration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientAssemblyMigration" /> class.
        /// </summary>
        /// <param name="fromAssembly">From assembly.</param>
        /// <param name="fromType">From type.</param>
        /// <param name="toType">To type.</param>
        public ClientAssemblyMigration(string fromAssembly, string fromType, Type toType)
        {
            this.FromAssembly = fromAssembly;
            this.FromType = fromType;
            this.ToType = toType;
        }

        /// <summary>
        /// Original assembly.
        /// </summary>
        public string FromAssembly { get; }

        /// <summary>
        /// Original type.
        /// </summary>
        public string FromType { get; }

        /// <summary>
        /// Type to bind to.
        /// </summary>
        public Type ToType { get; }
    }
}
