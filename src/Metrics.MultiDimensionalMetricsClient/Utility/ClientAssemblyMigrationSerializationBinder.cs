//-------------------------------------------------------------------------------------------------
// <copyright file="ClientAssemblyMigrationSerializationBinder.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System;
    using System.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Binds the client assembly to an assembly known to the server.
    /// </summary>
    internal class ClientAssemblyMigrationSerializationBinder : DefaultSerializationBinder
    {
        private readonly ClientAssemblyMigration[] migrations;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientAssemblyMigrationSerializationBinder"/> class.
        /// </summary>
        /// <param name="migrations">The migrations.</param>
        public ClientAssemblyMigrationSerializationBinder(ClientAssemblyMigration[] migrations)
        {
            this.migrations = migrations;
        }

        /// <summary>
        /// Determines what type the serialized data should be bound to.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>The type to bind the data to.</returns>
        public override Type BindToType(string assemblyName, string typeName)
        {
            var migration = this.migrations.SingleOrDefault(p => p.FromAssembly == assemblyName && p.FromType == typeName);
            if (migration != null)
            {
                return migration.ToType;
            }

            return base.BindToType(assemblyName, typeName);
        }
    }
}
