//-------------------------------------------------------------------------------------------------
// <copyright file="MetricValueV2.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents metric value as union of Long, Ulong, Double.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct MetricValueV2
    {
        /// <summary>
        /// The value as double datatype.
        /// </summary>
        [FieldOffset(0)]
        public double ValueAsDouble;

        /// <summary>
        /// The value as long datatype.
        /// </summary>
        [FieldOffset(0)]
        public long ValueAsLong;

        /// <summary>
        /// The value as unsigned long datatype.
        /// </summary>
        [FieldOffset(0)]
        public ulong ValueAsULong;

        public static explicit operator MetricValueV2(ulong v)
        {
            return new MetricValueV2 { ValueAsULong = v };
        }

        public static explicit operator MetricValueV2(long v)
        {
            return new MetricValueV2 { ValueAsLong = v };
        }

        public static explicit operator MetricValueV2(int v)
        {
            return new MetricValueV2 { ValueAsLong = v };
        }

        public static explicit operator MetricValueV2(uint v)
        {
            return new MetricValueV2 { ValueAsLong = v };
        }

        public static explicit operator MetricValueV2(double v)
        {
            return new MetricValueV2 { ValueAsDouble = v };
        }

        /// <summary>
        /// Determines whether this instance value for double can be represented as long
        /// </summary>
        /// <returns>True if the value can be represented as long.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanRepresentDoubleAsLong()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return this.ValueAsDouble - (long)this.ValueAsDouble == 0;
        }
    }
}