// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Specifies the integrity checksum type for LZMA/XZ compression.</summary>
    public enum XzChecksumType
    {
        /// <summary>No integrity checksum is calculated.</summary>
        None = 0,

        /// <summary>CRC32 using the polynomial from the IEEE 802.3 standard (4 bytes).</summary>
        Crc32 = 1,

        /// <summary>CRC64 using the polynomial from the ECMA-182 standard (8 bytes).</summary>
        Crc64 = 2,

        /// <summary>SHA-256 hash (32 bytes).</summary>
        Sha256 = 3,
    }
}
