//-------------------------------------------------------------------------------------------------
// <copyright file="OperationStatus.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Configuration
{
    /// <summary>
    /// Operation result status Type.
    /// </summary>
    public enum OperationStatus
    {
        CompleteSuccess = 0,
        ResourceNotFound = 1,
        ConnectionError = 2,
        FolderCreationError = 3,
        FileSaveError = 4,
        ResourceGetError = 5,
        ResourcePostError = 6,
        ResourceSkipped = 7,
        FileCorrupted = 8
    }
}