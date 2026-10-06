// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.Compression
{
    internal static partial class BrotliUtils
    {
        public const int WindowBits_Min = 10;
        public const int WindowBits_Default = 22;
        public const int WindowBits_Max = 24;
        /// <summary>The maximum window size supported by the "Large Window Brotli" extension (BROTLI_LARGE_MAX_WINDOW_BITS).</summary>
        public const int WindowBits_MaxLarge = 30;
        public const int Quality_Min = 0;
        public const int Quality_Default = 4;
        public const int Quality_Max = 11;
        /// <summary>
        /// The highest quality that uses static entropy codes (MAX_QUALITY_FOR_STATIC_ENTROPY_CODES). The native encoder silently
        /// disables large-window mode at or below this quality, clamping the window back to <see cref="WindowBits_Max"/>.
        /// </summary>
        public const int Quality_MaxForStaticEntropyCodes = 2;

        /// <summary>
        /// Reads the window size declared by the header of a Brotli stream without consuming any input.
        /// </summary>
        /// <param name="source">A buffer positioned at the start of a Brotli stream.</param>
        /// <param name="windowBits">When this method returns <see cref="OperationStatus.Done"/>, the declared base-2 logarithm of the window size.</param>
        /// <returns>
        /// <see cref="OperationStatus.Done"/> if the window size was determined; <see cref="OperationStatus.NeedMoreData"/> if
        /// <paramref name="source"/> is too short to determine it; otherwise <see cref="OperationStatus.InvalidData"/>.
        /// </returns>
        /// <remarks>
        /// The header is read least-significant-bit first and occupies at most 14 bits. This mirrors DecodeWindowBits and the
        /// BROTLI_STATE_LARGE_WINDOW_BITS state of the native decoder.
        /// </remarks>
        internal static OperationStatus TryGetWindowBits(ReadOnlySpan<byte> source, out int windowBits)
        {
            // The header is a variable-length prefix, read least-significant-bit first:
            //   bit 0 == 0         -> window is 16
            //   bits 1..3 (n) != 0 -> window is 17 + n (18..24)
            //   bits 4..6 (n) == 1 -> "Large Window Brotli" escape: bit 7 is reserved and must be zero,
            //                         and bits 8..13 hold the window size (10..30)
            //   bits 4..6 (n) > 1  -> window is 8 + n (10..15)
            //   bits 4..6 (n) == 0 -> window is 17
            const int LargeWindowEscape = 1;
            const int LargeWindowBitCount = 6;

            windowBits = 0;

            if (source.IsEmpty)
            {
                return OperationStatus.NeedMoreData;
            }

            int header = source[0];
            if ((header & 0b1) == 0)
            {
                windowBits = 16;
                return OperationStatus.Done;
            }

            int value = (header >> 1) & 0b111;
            if (value != 0)
            {
                windowBits = 17 + value;
                return OperationStatus.Done;
            }

            value = (header >> 4) & 0b111;
            if (value != LargeWindowEscape)
            {
                windowBits = value != 0 ? 8 + value : 17;
                return OperationStatus.Done;
            }

            // The escape is followed by a reserved bit that must be zero, and then by the window size itself.
            if ((header & 0b1000_0000) != 0)
            {
                return OperationStatus.InvalidData;
            }

            if (source.Length < 2)
            {
                return OperationStatus.NeedMoreData;
            }

            windowBits = source[1] & ((1 << LargeWindowBitCount) - 1);
            return windowBits is < WindowBits_Min or > WindowBits_MaxLarge
                ? OperationStatus.InvalidData
                : OperationStatus.Done;
        }

        /// <summary>
        /// Validates that a large window size is paired with a quality that the native encoder will honor.
        /// </summary>
        internal static void ValidateLargeWindowQuality(int quality, int windowLog2)
        {
            if (windowLog2 > WindowBits_Max && quality <= Quality_MaxForStaticEntropyCodes)
            {
                throw new ArgumentException(SR.Format(SR.BrotliEncoder_LargeWindowQuality, windowLog2, quality, Quality_MaxForStaticEntropyCodes));
            }
        }

        internal static int GetQualityFromCompressionLevel(CompressionLevel compressionLevel) =>
            compressionLevel switch
            {
                CompressionLevel.NoCompression => Quality_Min,
                CompressionLevel.Fastest => 1,
                CompressionLevel.Optimal => Quality_Default,
                CompressionLevel.SmallestSize => Quality_Max,
                _ => throw new ArgumentOutOfRangeException(nameof(compressionLevel), compressionLevel, SR.ArgumentOutOfRange_Enum)
            };
    }
}
