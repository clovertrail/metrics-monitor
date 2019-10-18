// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpecialCharsHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Utility
{
    using System;

    /// <summary>
    /// Helper class to escape and un-escape special chars
    /// </summary>
    public class SpecialCharsHelper
    {
        /// <summary>
        /// Eescape the input string twice.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The escaped string</returns>
        public static string EscapeTwice(string input)
        {
            return Uri.EscapeDataString(Uri.EscapeDataString(input));
        }
    }
}