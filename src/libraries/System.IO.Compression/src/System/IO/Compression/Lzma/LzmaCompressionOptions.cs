// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Provides compression options to be used with LZMA compression.</summary>
    public sealed class LzmaCompressionOptions
    {
        /// <summary>Gets the default compression preset level.</summary>
        public static int DefaultPreset => LzmaUtils.PresetDefault;

        /// <summary>Gets the minimum compression preset level.</summary>
        public static int MinPreset => LzmaUtils.PresetMin;

        /// <summary>Gets the maximum compression preset level.</summary>
        public static int MaxPreset => LzmaUtils.PresetMax;

        /// <summary>Gets the minimum dictionary size in bytes.</summary>
        public static int MinDictionarySize => LzmaUtils.DictSizeMin;

        /// <summary>Gets the maximum dictionary size in bytes (1.5 GiB).</summary>
        public static int MaxDictionarySize => LzmaUtils.DictSizeMax;

        /// <summary>Gets the default dictionary size in bytes (8 MiB).</summary>
        public static int DefaultDictionarySize => LzmaUtils.DictSizeDefault;

        private int _preset = LzmaUtils.PresetDefault;
        private int _dictionarySize;

        /// <summary>Initializes a new instance of the <see cref="LzmaCompressionOptions"/> class.</summary>
        public LzmaCompressionOptions()
        {
        }

        /// <summary>Gets or sets the compression preset level.</summary>
        /// <value>The compression preset level. The valid range is from <see cref="MinPreset"/> to <see cref="MaxPreset"/>.</value>
        /// <remarks>
        /// Higher preset levels provide better compression ratios but are slower and use more memory.
        /// <list type="bullet">
        ///   <item><description>Preset 0-3: Fast compression with lower memory usage</description></item>
        ///   <item><description>Preset 4-6: Balanced compression (default is 6)</description></item>
        ///   <item><description>Preset 7-9: Maximum compression with higher memory usage</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not between <see cref="MinPreset"/> and <see cref="MaxPreset"/>.</exception>
        public int Preset
        {
            get => _preset;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, MinPreset, nameof(value));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxPreset, nameof(value));

                _preset = value;
            }
        }

        /// <summary>Gets or sets whether to use extreme compression mode.</summary>
        /// <value><see langword="true"/> to use extreme compression; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
        /// <remarks>
        /// Extreme mode modifies the preset to make encoding significantly slower
        /// while improving the compression ratio only marginally. This is useful
        /// when you don't mind spending extra time to get the smallest result possible.
        /// Extreme mode does not significantly affect decoder memory usage.
        /// </remarks>
        public bool Extreme { get; set; }

        /// <summary>Gets or sets the integrity check type for the compressed data.</summary>
        /// <value>The integrity check type. The default is <see cref="LzmaCheck.Crc64"/>.</value>
        /// <remarks>
        /// The check is calculated from the uncompressed data and stored in the .xz container.
        /// <list type="bullet">
        ///   <item><description><see cref="LzmaCheck.None"/>: No integrity check (fastest, smallest output)</description></item>
        ///   <item><description><see cref="LzmaCheck.Crc32"/>: CRC32 (4 bytes overhead)</description></item>
        ///   <item><description><see cref="LzmaCheck.Crc64"/>: CRC64 (8 bytes overhead, recommended)</description></item>
        ///   <item><description><see cref="LzmaCheck.Sha256"/>: SHA-256 (32 bytes overhead, most secure)</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not a valid <see cref="LzmaCheck"/> value.</exception>
        public LzmaCheck Check { get; set; } = LzmaCheck.Crc64;

        /// <summary>Gets or sets the dictionary size in bytes.</summary>
        /// <value>The dictionary size in bytes. A value of 0 means the default size based on the preset will be used.</value>
        /// <remarks>
        /// The dictionary size determines how many bytes of recently processed uncompressed data
        /// is kept in memory for finding repeated patterns. Larger dictionaries can improve
        /// compression ratios but require more memory for both compression and decompression.
        /// The valid range is from <see cref="MinDictionarySize"/> to <see cref="MaxDictionarySize"/>,
        /// or 0 to use the default size determined by the preset.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is not 0 and is not between <see cref="MinDictionarySize"/> and <see cref="MaxDictionarySize"/>.</exception>
        public int DictionarySize
        {
            get => _dictionarySize;
            set
            {
                if (value != 0)
                {
                    ArgumentOutOfRangeException.ThrowIfLessThan(value, MinDictionarySize, nameof(value));
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxDictionarySize, nameof(value));
                }

                _dictionarySize = value;
            }
        }

        /// <summary>Gets the effective preset value including the extreme flag.</summary>
        internal uint GetEffectivePreset()
        {
            uint preset = (uint)_preset;
            if (Extreme)
            {
                preset |= LzmaUtils.PresetExtreme;
            }

            return preset;
        }
    }

    /// <summary>Specifies the integrity check type for LZMA/XZ compression.</summary>
    public enum LzmaCheck
    {
        /// <summary>No integrity check is calculated.</summary>
        None = (int)LzmaNative.LzmaCheck.None,

        /// <summary>CRC32 using the polynomial from the IEEE 802.3 standard (4 bytes).</summary>
        Crc32 = (int)LzmaNative.LzmaCheck.Crc32,

        /// <summary>CRC64 using the polynomial from the ECMA-182 standard (8 bytes).</summary>
        Crc64 = (int)LzmaNative.LzmaCheck.Crc64,

        /// <summary>SHA-256 hash (32 bytes).</summary>
        Sha256 = (int)LzmaNative.LzmaCheck.Sha256
    }
}
