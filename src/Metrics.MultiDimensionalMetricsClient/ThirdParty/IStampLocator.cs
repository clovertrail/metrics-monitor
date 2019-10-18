// <copyright file="IStampLocator.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Cloud.Metrics.Client.ThirdParty
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for locating the stamp endpoint for Azure external customers' 3rd party accounts.
    /// </summary>
    public interface IStampLocator
    {
        /// <summary>
        /// Gets the stamp endpoint.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="azureRegion">The azure region.</param>
        /// <returns>The stamp endpoint for the given account identified by <paramref name="subscriptionId"/>.</returns>
        Uri GetStampEndpoint(string subscriptionId, string azureRegion);

        /// <summary>
        /// Gets the stamp name.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="azureRegion">The azure region.</param>
        /// <returns>The stamp name for the given account identified by <paramref name="subscriptionId"/>.</returns>
        string GetStampName(string subscriptionId, string azureRegion);
    }
}