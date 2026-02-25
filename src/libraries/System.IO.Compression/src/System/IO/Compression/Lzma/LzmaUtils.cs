// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal static class LzmaUtils
    {
        // LZMA quality constants from lzma/container.h
        internal const int QualityMin = 0;
        internal const int QualityMax = 9;
        internal const int QualityDefault = 6;
        internal const uint QualityLevelMask = 0x1F;
        internal const uint QualityExtreme = 1U << 31;

        // Window log constants from lzma/lzma12.h
        // In LZMA, the window size (historically called "dictionary size") determines
        // how many bytes of recently processed uncompressed data the compressor can
        // reference for finding repeated patterns. This is the same concept as WindowLog
        // in Zstandard, expressed as base 2 logarithm (e.g. 23 means 2^23 = 8 MiB).
        internal const int WindowLogMin = 12;              // 2^12 = 4 KiB
        internal const int WindowLogDefault = 23;           // 2^23 = 8 MiB
        internal const int WindowLogMax = 30;               // 2^30 = 1 GiB

        /// <summary>Converts a WindowLog value (base 2 logarithm) to the actual window size in bytes.</summary>
        internal static int WindowSizeFromLog(int windowLog) => 1 << windowLog;

        // Buffer sizes for LZMA operations
        internal const int DefaultInternalBufferSize = (1 << 16) - 16; // 65520 bytes, similar to Brotli/Zstandard

        /// <summary>Checks if an LZMA operation result indicates an error.</summary>
        internal static bool IsError(LzmaNative.LzmaRetCode result) =>
            result != LzmaNative.LzmaRetCode.Ok && result != LzmaNative.LzmaRetCode.StreamEnd;

        /// <summary>Throws an exception if the LZMA operation result indicates an error.</summary>
        internal static void ThrowIfError(LzmaNative.LzmaRetCode result)
        {
            if (IsError(result))
            {
                ThrowForErrorCode(result);
            }
        }

        internal static void ThrowForErrorCode(LzmaNative.LzmaRetCode error)
        {
            string message = error switch
            {
                LzmaNative.LzmaRetCode.MemError => "Memory allocation failed.",
                LzmaNative.LzmaRetCode.MemlimitError => "Memory usage limit was exceeded.",
                LzmaNative.LzmaRetCode.FormatError => "The input is not in the expected format.",
                LzmaNative.LzmaRetCode.OptionsError => "Invalid or unsupported options.",
                LzmaNative.LzmaRetCode.DataError => "Data is corrupt or incomplete.",
                LzmaNative.LzmaRetCode.BufError => "No progress is possible (output buffer too small or input exhausted).",
                LzmaNative.LzmaRetCode.ProgError => "Programming error in the application.",
                LzmaNative.LzmaRetCode.UnsupportedCheck => "The specified integrity check is not supported.",
                LzmaNative.LzmaRetCode.GetCheck => "Integrity check type is now available.",
                LzmaNative.LzmaRetCode.SeekNeeded => "Seek is needed to continue decoding.",
                _ => $"Unknown LZMA error: {error}"
            };

            throw new IOException(message);
        }

        /// <summary>Gets the LZMA quality level from a CompressionLevel value.</summary>
        internal static int GetQualityFromCompressionLevel(CompressionLevel compressionLevel) =>
            compressionLevel switch
            {
                // LZMA quality levels range from 0-9:
                // 0-3: Fast compression with lower memory usage
                // 4-6: Balanced compression (default is 6)
                // 7-9: Maximum compression with higher memory usage
                CompressionLevel.NoCompression => QualityMin,
                CompressionLevel.Fastest => 1,
                CompressionLevel.Optimal => QualityDefault,
                CompressionLevel.SmallestSize => QualityMax,
                _ => throw new ArgumentOutOfRangeException(nameof(compressionLevel), compressionLevel, SR.ArgumentOutOfRange_Enum)
            };
    }
}
