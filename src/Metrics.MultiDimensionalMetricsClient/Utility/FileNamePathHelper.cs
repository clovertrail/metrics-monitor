//-------------------------------------------------------------------------------------------------
// <copyright file="FileNamePathHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// The helper class for file names and paths.
    /// </summary>
    internal static class FileNamePathHelper
    {
        /// <summary>
        /// The default maximum file name allowed.
        /// </summary>
        internal const int MaximumFileNameAllowed = 256;

        /// <summary>
        /// The json file extension.
        /// </summary>
        internal const string JsonFileExtension = ".json";

        /// <summary>
        /// The javascript file extension.
        /// </summary>
        private const string JsFileExtension = ".js";

        /// <summary>
        /// The sorted invalid file chars.
        /// </summary>
        private static readonly char[] SortedInvalidFileChars;

        /// <summary>
        /// Initializes static members of the <see cref="FileNamePathHelper"/> class.
        /// </summary>
        static FileNamePathHelper()
        {
            SortedInvalidFileChars = Path.GetInvalidFileNameChars();
            Array.Sort(SortedInvalidFileChars);
        }

        /// <summary>
        /// Constructs the name of the valid file by joining the provided parameters with "_" while replacing invalid chars with "^".
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metric">The metric.</param>
        /// <param name="monitorId">The monitor identifier.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <param name="maximumFileNameAllowed">The maximum length of the file name that will be returned.</param>
        /// <returns>
        /// A valid file name.
        /// </returns>
        [SuppressMessage("Microsoft.Security.Cryptography", "CA5354:SHA1CannotBeUsed", Justification = "Used for converting long string to short string, not for security or comparison")]
        internal static string ConstructValidFileName(string monitoringAccount, string metricNamespace, string metric, string monitorId, string fileExtension, int maximumFileNameAllowed)
        {
            // To provide better back compat, only modify if it exceeds the max allowed in the first place.
            var builder = new StringBuilder(maximumFileNameAllowed);
            builder.Append(monitoringAccount);

            if (!string.IsNullOrWhiteSpace(metricNamespace))
            {
                builder.Append('_').Append(metricNamespace);
            }

            if (!string.IsNullOrWhiteSpace(metric))
            {
                builder.Append('_').Append(metric);
            }

            if (!string.IsNullOrWhiteSpace(monitorId))
            {
                builder.Append('_').Append(monitorId);
            }

            var longFileNameString = builder.ToString();

            var requiresHashing = builder.Length + fileExtension.Length > maximumFileNameAllowed;
            if (requiresHashing)
            {
                builder.Clear();
                const int hashValueLength = 16;
                var totalNameLength = maximumFileNameAllowed - fileExtension.Length - hashValueLength - 1;

                // Construct the allowed space for each part remaining (monitoring account always is included)
                var providedParts = 1;
                if (!string.IsNullOrWhiteSpace(metricNamespace))
                {
                    ++providedParts;
                }

                if (!string.IsNullOrWhiteSpace(metric))
                {
                    ++providedParts;
                }

                if (!string.IsNullOrWhiteSpace(monitorId))
                {
                    ++providedParts;
                }

                AppendShortedFilePart(builder, monitoringAccount, totalNameLength, ref providedParts);
                AppendShortedFilePart(builder, metricNamespace, totalNameLength, ref providedParts);
                AppendShortedFilePart(builder, metric, totalNameLength, ref providedParts);
                AppendShortedFilePart(builder, monitorId, totalNameLength, ref providedParts);
            }

            RepalceInvalidFileChars(builder);

            if (!requiresHashing)
            {
                return builder.Append(fileExtension).ToString();
            }

            var shortFileNameString = builder.ToString();
            builder.Clear();
            using (var hashGenerator = SHA1.Create())
            {
                // Using only first 8 bytes of hash as 'good enough' in this case
                var data = hashGenerator.ComputeHash(Encoding.UTF8.GetBytes(longFileNameString.ToLowerInvariant()));
                for (var i = 0; i < data.Length && i < 8; ++i)
                {
                    builder.Append(data[i].ToString("x2"));
                }
            }

            return shortFileNameString + "_" + builder + fileExtension;
        }

        /// <summary>
        /// Constructs the name of the valid file by joining the provided parameters with "_" while replacing invalid chars with "^".
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="maximumFileNameAllowed">The maximum length of the file name that will be returned.</param>
        /// <returns>
        /// A valid file name.
        /// </returns>
        internal static string ConstructValidFileName(string monitoringAccount, int maximumFileNameAllowed)
        {
            return ConstructValidFileName(monitoringAccount, string.Empty, string.Empty, string.Empty, JsonFileExtension, maximumFileNameAllowed);
        }

        /// <summary>
        /// Convert path to valid folder name
        /// </summary>
        /// <param name="path">The path needs to be converted</param>
        /// <returns>Valid folder name</returns>
        internal static string ConvertPathToValidFolderName(string path)
        {
            var builder = new StringBuilder(path);
            RepalceInvalidFileChars(builder);
            return builder.ToString();
        }

        /// <summary>
        /// Appends the shorted file part to the final string.
        /// </summary>
        /// <param name="builder">The builder containing the final string.</param>
        /// <param name="value">The value to add to the string.</param>
        /// <param name="totalAllowedLength">Total length of the final string allowed.</param>
        /// <param name="partsRemaining">The parts remaining that will be added to the final string.</param>
        private static void AppendShortedFilePart(StringBuilder builder, string value, int totalAllowedLength, ref int partsRemaining)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Do not count this as a part since it was empty to begin with.
                return;
            }

            var currentMaxAllowed = (totalAllowedLength - builder.Length) / partsRemaining;
            --partsRemaining;
            if (value.Length > currentMaxAllowed)
            {
                var partLengths = (currentMaxAllowed - 1) / 2;
                builder.AppendFormat(
                    "{0}{1}~{2}",
                    builder.Length == 0 ? string.Empty : "_",
                    value.Substring(0, partLengths),
                    value.Substring(value.Length - partLengths));
            }
            else
            {
                builder.AppendFormat(
                    "{0}{1}",
                    builder.Length == 0 ? string.Empty : "_",
                    value);
            }
        }

        /// <summary>
        /// Replace invalid file characters
        /// </summary>
        /// <param name="builder">string builder</param>
        private static void RepalceInvalidFileChars(StringBuilder builder)
        {
            for (int i = 0; i < builder.Length; ++i)
            {
                if (!char.IsLetter(builder[i]) && !char.IsDigit(builder[i]))
                {
                    if (Array.BinarySearch(SortedInvalidFileChars, builder[i]) >= 0)
                    {
                        builder[i] = '^';
                    }
                }
            }
        }
    }
}