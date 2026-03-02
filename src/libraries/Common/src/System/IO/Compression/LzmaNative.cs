// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO.Compression
{
    /// <summary>
    /// This class provides declarations for constants and structures for exposing
    /// the native liblzma library to managed code.
    /// </summary>
    internal static class LzmaNative
    {
        /// <summary>Return codes from liblzma functions.</summary>
        internal enum LzmaRetCode : uint
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
        internal enum LzmaAction : uint
        {
            /// <summary>Continue encoding/decoding.</summary>
            Run = 0,

            /// <summary>Make all buffered data available at output.</summary>
            SyncFlush = 1,

            /// <summary>Flush and reset encoder state (compression only).</summary>
            FullFlush = 2,

            /// <summary>Finish the encoding/decoding.</summary>
            Finish = 3,

            /// <summary>Flush and set end-of-stream marker (compression only).</summary>
            FullBarrier = 4
        }

        /// <summary>Integrity check type for LZMA/XZ compression.</summary>
        internal enum LzmaCheck : uint
        {
            /// <summary>No integrity check is calculated.</summary>
            None = 0,

            /// <summary>CRC32 using the polynomial from the IEEE 802.3 standard.</summary>
            Crc32 = 1,

            /// <summary>CRC64 using the polynomial from the ECMA-182 standard.</summary>
            Crc64 = 4,

            /// <summary>SHA-256 hash.</summary>
            Sha256 = 10
        }

        /// <summary>
        /// The lzma_stream structure is used for passing data to and from liblzma.
        /// This structure must be zeroed before first use.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct LzmaStream
        {
            /// <summary>Pointer to the next input byte.</summary>
            public byte* NextIn;

            /// <summary>Number of available input bytes in NextIn.</summary>
            public nuint AvailIn;

            /// <summary>Total number of bytes read by liblzma.</summary>
            public ulong TotalIn;

            /// <summary>Pointer to the next output position.</summary>
            public byte* NextOut;

            /// <summary>Amount of free space in NextOut.</summary>
            public nuint AvailOut;

            /// <summary>Total number of bytes written by liblzma.</summary>
            public ulong TotalOut;

            /// <summary>Custom memory allocation functions. Usually NULL.</summary>
            public void* Allocator;

            /// <summary>Internal state pointer. Do not modify.</summary>
            public void* Internal;

            // Reserved members for ABI compatibility
            private void* _reservedPtr1;
            private void* _reservedPtr2;
            private void* _reservedPtr3;
            private void* _reservedPtr4;
            private ulong _seekPos;
            private ulong _reservedInt2;
            private nuint _reservedInt3;
            private nuint _reservedInt4;
            private uint _reservedEnum1;
            private uint _reservedEnum2;
        }

        /// <summary>LZMA2 filter ID.</summary>
        internal const ulong FilterLzma2 = 0x21;

        /// <summary>Sentinel value used to terminate a filter chain.</summary>
        internal const ulong VliUnknown = ulong.MaxValue;

        /// <summary>
        /// Filter options structure for lzma_stream_encoder.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct LzmaFilter
        {
            /// <summary>Filter ID.</summary>
            public ulong Id;

            /// <summary>Filter-specific options (can be NULL for default options).</summary>
            public void* Options;
        }

        /// <summary>
        /// Options for the LZMA1 and LZMA2 filters, corresponding to lzma_options_lzma in lzma/lzma12.h.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct LzmaOptionsLzma
        {
            /// <summary>Dictionary size in bytes (the window size).</summary>
            public uint DictSize;

            /// <summary>Pointer to an initial dictionary (usually null).</summary>
            public byte* PresetDict;

            /// <summary>Size of the preset dictionary.</summary>
            public uint PresetDictSize;

            /// <summary>Number of literal context bits (lc).</summary>
            public uint Lc;

            /// <summary>Number of literal position bits (lp).</summary>
            public uint Lp;

            /// <summary>Number of position bits (pb).</summary>
            public uint Pb;

            /// <summary>Compression mode (lzma_mode enum).</summary>
            public uint Mode;

            /// <summary>Nice match length.</summary>
            public uint NiceLen;

            /// <summary>Match finder algorithm (lzma_match_finder enum).</summary>
            public uint Mf;

            /// <summary>Maximum search depth.</summary>
            public uint Depth;

            /// <summary>Extension flags.</summary>
            public uint ExtFlags;

            /// <summary>Extension size low bits.</summary>
            public uint ExtSizeLow;

            /// <summary>Extension size high bits.</summary>
            public uint ExtSizeHigh;

            // Reserved members for ABI compatibility (must match lzma_options_lzma layout)
            private uint _reservedInt4;
            private uint _reservedInt5;
            private uint _reservedInt6;
            private uint _reservedInt7;
            private uint _reservedInt8;
            private uint _reservedEnum1;
            private uint _reservedEnum2;
            private uint _reservedEnum3;
            private uint _reservedEnum4;
            private void* _reservedPtr1;
            private void* _reservedPtr2;
        }
    }
}
