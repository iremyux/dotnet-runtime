// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Specifies the compression method used to compress an entry in a zip archive.
    /// </summary>
    /// <remarks>
    /// The values correspond to the compression method values described in the ZIP File Format Specification (APPNOTE.TXT section 4.4.5).
    /// Not all values are natively supported for decompression by <see cref="ZipArchiveEntry.Open()"/>. Use <see cref="ZipArchiveEntry.OpenRaw"/>
    /// to access raw compressed data for entries using unsupported compression methods.
    /// </remarks>
    public enum ZipCompressionMethod
    {
        /// <summary>
        /// The entry is stored (no compression).
        /// </summary>
        Stored = 0x0,

        /// <summary>
        /// The entry is compressed using the Deflate algorithm.
        /// </summary>
        Deflate = 0x8,

        /// <summary>
        /// The entry is compressed using the Deflate64 algorithm.
        /// </summary>
        Deflate64 = 0x9,

        /// <summary>
        /// The entry is compressed using the BZip2 algorithm.
        /// </summary>
        BZip2 = 0xC,

        /// <summary>
        /// The entry is compressed using the LZMA algorithm.
        /// </summary>
        Lzma = 0xE,

        /// <summary>
        /// The entry is compressed using the Zstandard algorithm.
        /// </summary>
        Zstd = 0x5D,
    }
}
