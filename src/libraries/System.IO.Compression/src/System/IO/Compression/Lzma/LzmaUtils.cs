// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal static class LzmaUtils
    {
        // LZMA preset constants from lzma/container.h
        internal const int PresetMin = 0;
        internal const int PresetMax = 9;
        internal const int PresetDefault = 6;
        internal const uint PresetLevelMask = 0x1F;
        internal const uint PresetExtreme = 1U << 31;

        // Dictionary size constants from lzma/lzma12.h
        internal const uint DictSizeMin = 4096;
        internal const uint DictSizeDefault = 1U << 23; // 8 MiB
        internal const uint DictSizeMax = (1U << 30) + (1U << 29); // 1.5 GiB

        // Buffer sizes for LZMA operations
        internal const int DefaultInternalBufferSize = (1 << 16) - 16; // 65520 bytes, similar to Brotli/Zstandard

        /// <summary>Checks if an LZMA operation result indicates an error.</summary>
        internal static bool IsError(LzmaReturnCode result) => result != LzmaReturnCode.Ok && result != LzmaReturnCode.StreamEnd;

        /// <summary>Throws an exception if the LZMA operation result indicates an error.</summary>
        internal static void ThrowIfError(LzmaReturnCode result)
        {
            if (IsError(result))
            {
                ThrowForErrorCode(result);
            }
        }

        internal static void ThrowForErrorCode(LzmaReturnCode error)
        {
            string message = error switch
            {
                LzmaReturnCode.MemError => "Memory allocation failed.",
                LzmaReturnCode.MemlimitError => "Memory usage limit was exceeded.",
                LzmaReturnCode.FormatError => "The input is not in the expected format.",
                LzmaReturnCode.OptionsError => "Invalid or unsupported options.",
                LzmaReturnCode.DataError => "Data is corrupt or incomplete.",
                LzmaReturnCode.BufError => "No progress is possible (output buffer too small or input exhausted).",
                LzmaReturnCode.ProgError => "Programming error in the application.",
                LzmaReturnCode.UnsupportedCheck => "The specified integrity check is not supported.",
                LzmaReturnCode.GetCheck => "Integrity check type is now available.",
                LzmaReturnCode.SeekNeeded => "Seek is needed to continue decoding.",
                _ => $"Unknown LZMA error: {error}"
            };

            throw new IOException(message);
        }
    }

    /// <summary>Return codes from liblzma functions.</summary>
    internal enum LzmaReturnCode
    {
        /// <summary>Operation completed successfully.</summary>
        Ok = 0,

        /// <summary>End of stream was reached.</summary>
        StreamEnd = 1,

        /// <summary>Input stream has no integrity check.</summary>
        NoCheck = 2,

        /// <summary>Cannot calculate the integrity check.</summary>
        UnsupportedCheck = 3,

        /// <summary>Integrity check type is now available.</summary>
        GetCheck = 4,

        /// <summary>Memory allocation failed.</summary>
        MemError = 5,

        /// <summary>Memory usage limit was exceeded.</summary>
        MemlimitError = 6,

        /// <summary>File format not recognized.</summary>
        FormatError = 7,

        /// <summary>Invalid or unsupported options.</summary>
        OptionsError = 8,

        /// <summary>Data is corrupt.</summary>
        DataError = 9,

        /// <summary>No progress is possible.</summary>
        BufError = 10,

        /// <summary>Programming error.</summary>
        ProgError = 11,

        /// <summary>Seek is needed.</summary>
        SeekNeeded = 12
    }

    /// <summary>Action codes for lzma_code function.</summary>
    internal enum LzmaAction
    {
        /// <summary>Continue encoding/decoding.</summary>
        Run = 0,

        /// <summary>Make all buffered data available at output.</summary>
        SyncFlush = 1,

        /// <summary>Flush and reset encoder state (compression only).</summary>
        FullFlush = 2,

        /// <summary>Flush and set end-of-stream marker (compression only).</summary>
        FullBarrier = 4,

        /// <summary>Finish the encoding/decoding.</summary>
        Finish = 3
    }
}
