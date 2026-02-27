// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Provides compression options to be used with LZMA compression.</summary>
    public sealed class LzmaCompressionOptions
    {
        /// <summary>Gets the default compression quality level.</summary>
        public static int DefaultQuality => LzmaUtils.QualityDefault;

        /// <summary>Gets the minimum compression quality level.</summary>
        public static int MinQuality => LzmaUtils.QualityMin;

        /// <summary>Gets the maximum compression quality level.</summary>
        public static int MaxQuality => LzmaUtils.QualityMax;

        /// <summary>Gets the minimum window log value.</summary>
        public static int MinWindowLog => LzmaUtils.WindowLogMin;

        /// <summary>Gets the maximum window log value.</summary>
        public static int MaxWindowLog => LzmaUtils.WindowLogMax;

        /// <summary>Gets the default window log value.</summary>
        public static int DefaultWindowLog => LzmaUtils.WindowLogDefault;

        private int _quality = LzmaUtils.QualityDefault;
        private int _windowLog;

        /// <summary>Initializes a new instance of the <see cref="LzmaCompressionOptions"/> class.</summary>
        public LzmaCompressionOptions()
        {
        }

        /// <summary>Gets or sets the compression quality level.</summary>
        /// <value>The compression quality level. The valid range is from <see cref="MinQuality"/> to <see cref="MaxQuality"/>.</value>
        /// <remarks>
        /// Higher quality levels provide better compression ratios but are slower and use more memory.
        /// <list type="bullet">
        ///   <item><description>Quality 0-3: Fast compression with lower memory usage</description></item>
        ///   <item><description>Quality 4-6: Balanced compression (default is 6)</description></item>
        ///   <item><description>Quality 7-9: Maximum compression with higher memory usage</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not between <see cref="MinQuality"/> and <see cref="MaxQuality"/>.</exception>
        public int Quality
        {
            get => _quality;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, MinQuality, nameof(value));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxQuality, nameof(value));

                _quality = value;
            }
        }

        /// <summary>Gets or sets whether to enable extreme compression mode.</summary>
        /// <value><see langword="true"/> to enable extreme compression; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
        /// <remarks>
        /// Extreme mode modifies the preset to make encoding significantly slower
        /// while improving the compression ratio only marginally. This is useful
        /// when you don't mind spending extra time to get the smallest result possible.
        /// Extreme mode does not significantly affect decoder memory usage.
        /// </remarks>
        public bool EnableExtremeMode { get; set; }

        /// <summary>Gets or sets the integrity checksum type for the compressed data.</summary>
        /// <value>The integrity checksum type. The default is <see cref="LzmaChecksum.Crc64"/>.</value>
        /// <remarks>
        /// The checksum is calculated from the uncompressed data and stored in the .xz container.
        /// <list type="bullet">
        ///   <item><description><see cref="LzmaChecksum.None"/>: No integrity checksum (fastest, smallest output)</description></item>
        ///   <item><description><see cref="LzmaChecksum.Crc32"/>: CRC32 (4 bytes overhead)</description></item>
        ///   <item><description><see cref="LzmaChecksum.Crc64"/>: CRC64 (8 bytes overhead, recommended)</description></item>
        ///   <item><description><see cref="LzmaChecksum.Sha256"/>: SHA-256 (32 bytes overhead, most secure)</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not a valid <see cref="LzmaChecksum"/> value.</exception>
        public LzmaChecksum Checksum { get; set; } = LzmaChecksum.Crc64;

        /// <summary>Gets or sets the window size, expressed as base 2 logarithm.</summary>
        /// <value>The window size for compression, expressed as base 2 logarithm. A value of 0 means the default size based on the preset will be used.</value>
        /// <remarks>
        /// The window size determines how many bytes of recently processed uncompressed data
        /// is kept in memory for finding repeated patterns. Larger windows can improve
        /// compression ratios but require more memory for both compression and decompression.
        /// This is equivalent to what the LZMA format calls "dictionary size".
        /// The valid range is from <see cref="MinWindowLog"/> to <see cref="MaxWindowLog"/>,
        /// or 0 to use the default size determined by the preset.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not 0 and is not between <see cref="MinWindowLog"/> and <see cref="MaxWindowLog"/>.</exception>
        public int WindowLog
        {
            get => _windowLog;
            set
            {
                if (value != 0)
                {
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, MinWindowLog, nameof(value));
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxWindowLog, nameof(value));
                }

                _windowLog = value;
            }
        }

        /// <summary>Gets the effective quality value including the extreme flag.</summary>
        internal uint GetEffectiveQuality()
        {
            uint quality = (uint)_quality;
            if (EnableExtremeMode)
            {
                quality |= LzmaUtils.QualityExtreme;
            }

            return quality;
        }
    }

    /// <summary>Specifies the integrity checksum type for LZMA/XZ compression.</summary>
    public enum LzmaChecksum
    {
        /// <summary>No integrity checksum is calculated.</summary>
        None = (int)LzmaNative.LzmaCheck.None,

        /// <summary>CRC32 using the polynomial from the IEEE 802.3 standard (4 bytes).</summary>
        Crc32 = (int)LzmaNative.LzmaCheck.Crc32,

        /// <summary>CRC64 using the polynomial from the ECMA-182 standard (8 bytes).</summary>
        Crc64 = (int)LzmaNative.LzmaCheck.Crc64,

        /// <summary>SHA-256 hash (32 bytes).</summary>
        Sha256 = (int)LzmaNative.LzmaCheck.Sha256
    }
}
