// -----------------------------------------------------------------------
// <copyright file="EtwPayloadManipulationUtils.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// This class contains functions used for metrics data serialization/deserialization to/from the ETW payload.
    /// </summary>
    internal static unsafe class EtwPayloadManipulationUtils
    {
        /// <summary>
        /// Equivalent of + operator for IntPtr.
        /// </summary>
        /// <param name="ptr">Pointer value.</param>
        /// <param name="offset">Offset to add.</param>
        /// <returns>Incremented pointer value.</returns>
        public static IntPtr Shift(IntPtr ptr, int offset)
        {
            return new IntPtr(ptr.ToInt64() + offset);
        }

        /// <summary>
        /// Writes a string in UTF8 format to a buffer starting from the specified pointer.
        /// The format is: string size in bytes (int), UTF8 string bytes.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="pointerInPayload">Pointer to a buffer.</param>
        /// <param name="bytesBuffer">Buffer to use during string encoding.</param>
        /// <returns>A pointer shifted by number of bytes written to a buffer.</returns>
        public static IntPtr WriteString(string value, IntPtr pointerInPayload, byte[] bytesBuffer)
        {
            var bytesCount = Encoding.UTF8.GetBytes(value, 0, value.Length, bytesBuffer, 0);
            *((ushort*)pointerInPayload) = (ushort)bytesCount;
            pointerInPayload = new IntPtr(pointerInPayload.ToInt64() + sizeof(ushort));
            Marshal.Copy(bytesBuffer, 0, pointerInPayload, bytesCount);
            return new IntPtr(pointerInPayload.ToInt64() + bytesCount);
        }

        /// <summary>
        /// Reads string value encoded in buffer in UTF8 format.
        /// </summary>
        /// <param name="pointerInPayload">A pointer to a buffer where string bytes are stored.
        /// It will be updated to offset equal to number of bytes occupied by the string.</param>
        /// <returns>String values read.</returns>
        public static string ReadString(ref IntPtr pointerInPayload)
        {
            ushort strLen = *((ushort*)pointerInPayload);
            pointerInPayload = new IntPtr(pointerInPayload.ToInt64() + sizeof(ushort));
            var stringOnPayload = new string((sbyte*)pointerInPayload, 0, strLen, Encoding.UTF8);
            pointerInPayload = new IntPtr(pointerInPayload.ToInt64() + strLen);
            return stringOnPayload;
        }
    }
}
