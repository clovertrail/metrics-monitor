//-------------------------------------------------------------------------------------------------
// <copyright file="BitAggregateMagic.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization.BitHelper
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The Aggregate Magic Algorithms adapted from @ http://aggregate.org/MAGIC/
    /// </summary>
    public static class BitAggregateMagic
    {
        /// <summary>
        /// The number of bits in long integer.
        /// </summary>
        public const byte NumBitsInLongInteger = 64;

        /// <summary>
        /// Counts the number of set bits in <paramref name="x"/>.
        /// </summary>
        /// <param name="x">The target for which to count the number of set bits.</param>
        /// <returns>The number of set bits in <paramref name="x"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOneBits(ulong x)
        {
            x = (x & 0x5555555555555555) + ((x >> 1) & 0x5555555555555555);
            x = (x & 0x3333333333333333) + ((x >> 2) & 0x3333333333333333);
            x = (x & 0x0f0f0f0f0f0f0f0f) + ((x >> 4) & 0x0f0f0f0f0f0f0f0f);
            x = (x & 0x00ff00ff00ff00ff) + ((x >> 8) & 0x00ff00ff00ff00ff);
            x = (x & 0x0000ffff0000ffff) + ((x >> 16) & 0x0000ffff0000ffff);
            x = (x & 0x00000000ffffffff) + ((x >> 32) & 0x00000000ffffffff);

            return (int)x;
        }

        /// <summary>
        /// Counts the number of leading zeros.
        /// </summary>
        /// <param name="x">The target for which to count the number of leading zeros.</param>
        /// <returns>The number of leading zeros.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountLeadingZeros(long x)
        {
            if (x < 0)
            {
                return 0;
            }

            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x |= x >> 32;

            return NumBitsInLongInteger - CountOneBits((ulong)x);
        }

        /// <summary>
        /// Counts the number of trailing zeros.
        /// </summary>
        /// <param name="x">The target for which to count the number of trailing zeros.</param>
        /// <returns>The number of trailing zeros.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountTrailingZeros(long x)
        {
            return CountOneBits((ulong)(x & -x) - 1);
        }

        /// <summary>
        /// Given a binary integer value x, the next largest power of 2 can be computed by a SWAR
        /// algorithm that recursively "folds" the upper bits into the lower bits.
        /// This process yields a bit vector with the same most significant 1 as x, but all 1's below it.
        /// Adding 1 to that value yields the next largest power of 2.
        /// </summary>
        /// <param name="x">The value for which the next largest power of 2 is needed</param>
        /// <returns>The next largest power of 2 for x which is strictly more than x
        /// Note that for x = 32 the result is 64, not 32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextLargestPowerOf2(int x)
        {
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        /// <summary>
        /// Checks whether x is a power of two.
        /// </summary>
        /// <param name="x">The number to check,</param>
        /// <returns>true if x is a power of 2, false otherwise. (Note that 0 is a power of 2)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(ulong x)
        {
            return (x & (x - 1)) == 0;
        }

        /// <summary>
        /// Changes uinsigned int endian encoding.
        /// Can be used both ways: BE->LE and LE->BE.
        /// </summary>
        /// <param name="x">Value to change.</param>
        /// <returns>Value with changed endian encoding.</returns>
        public static uint ChangeEndianness(uint x)
        {
            // swap adjacent 16-bit blocks
            x = (x >> 16) | (x << 16);

            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        /// <summary>
        /// Changes uinsigned long endian encoding.
        /// Can be used both ways: BE->LE and LE->BE.
        /// </summary>
        /// <param name="x">Value to change.</param>
        /// <returns>Value with changed endian encoding.</returns>
        public static ulong ChangeEndianness(ulong x)
        {
            // swap adjacent 32-bit blocks
            x = (x >> 32) | (x << 32);

            // swap adjacent 16-bit blocks
            x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);

            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);
        }
    }
}
