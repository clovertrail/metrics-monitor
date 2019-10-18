//-------------------------------------------------------------------------------------------------
// <copyright file="FileOperationHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.Cloud.Metrics.Client.Logging;

    /// <summary>
    /// Helper class for file I/O
    /// </summary>
    internal static class FileOperationHelper
    {
        private static readonly object LogId = Logger.CreateCustomLogId("FileOperationHelper");

        /// <summary>
        /// Create folder if not existed
        /// </summary>
        /// <param name="folder">The folder</param>
        /// <returns>Whether the folder is successfully created</returns>
        internal static bool CreateFolderIfNotExists(string folder)
        {
            if (!Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception e)
                {
                    Logger.Log(LoggerLevel.Error, LogId, "CreateFolderIfNotExists", $"Fail to create folder {folder}. {e}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Save config to disk
        /// </summary>
        /// <param name="path">The path for saving the config</param>
        /// <param name="content">The content to be saved</param>
        internal static void SaveContentToFile(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
            }
            catch (Exception e)
            {
                Logger.Log(LoggerLevel.Error, LogId, "SaveConfig", $"Fail to save file {path}. {e}");
                throw;
            }
        }

        /// <summary>
        /// Async save config to disk
        /// </summary>
        /// <param name="path">The path for saving the config</param>
        /// <param name="content">The content to be saved</param>
        /// <returns>return a Task</returns>
        internal static async Task SaveContentToFileAsync(string path, string content)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    await writer.WriteAsync(content).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Log(LoggerLevel.Error, LogId, "SaveContentToFileAsync", $"Fail to save file {path}. {e}");
                throw;
            }
        }
    }
}