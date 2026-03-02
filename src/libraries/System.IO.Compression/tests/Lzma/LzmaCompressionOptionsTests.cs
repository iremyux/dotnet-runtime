// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Compression
{
    public class LzmaCompressionOptionsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            LzmaCompressionOptions options = new();

            Assert.Equal(LzmaCompressionOptions.DefaultQuality, options.Quality);
            Assert.Equal(0, options.WindowLog);
            Assert.False(options.EnableExtremeMode);
            Assert.Equal(LzmaChecksumType.Crc64, options.Checksum);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(9)]
        public void Quality_SetToValidRange_Succeeds(int quality)
        {
            LzmaCompressionOptions options = new();
            options.Quality = quality;
            Assert.Equal(quality, options.Quality);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(10)]
        public void Quality_SetOutOfRange_ThrowsArgumentOutOfRangeException(int quality)
        {
            LzmaCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.Quality = quality);
        }

        [Theory]
        [InlineData(12)]
        [InlineData(23)]
        [InlineData(30)]
        public void WindowLog_SetToValidRange_Succeeds(int windowLog)
        {
            LzmaCompressionOptions options = new();
            options.WindowLog = windowLog;
            Assert.Equal(windowLog, options.WindowLog);
        }

        [Fact]
        public void WindowLog_SetToZero_UsesDefault()
        {
            LzmaCompressionOptions options = new();
            options.WindowLog = 0;
            Assert.Equal(0, options.WindowLog);
        }

        [Theory]
        [InlineData(11)]
        [InlineData(31)]
        public void WindowLog_SetOutOfRange_ThrowsArgumentOutOfRangeException(int windowLog)
        {
            LzmaCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.WindowLog = windowLog);
        }

        [Fact]
        public void EnableExtremeMode_SetAndGet_Succeeds()
        {
            LzmaCompressionOptions options = new();
            options.EnableExtremeMode = true;
            Assert.True(options.EnableExtremeMode);

            options.EnableExtremeMode = false;
            Assert.False(options.EnableExtremeMode);
        }

        [Theory]
        [InlineData(LzmaChecksumType.None)]
        [InlineData(LzmaChecksumType.Crc32)]
        [InlineData(LzmaChecksumType.Crc64)]
        [InlineData(LzmaChecksumType.Sha256)]
        public void Checksum_SetToValidValues_Succeeds(LzmaChecksumType checksum)
        {
            LzmaCompressionOptions options = new();
            options.Checksum = checksum;
            Assert.Equal(checksum, options.Checksum);
        }

        [Fact]
        public void StaticProperties_ReturnExpectedValues()
        {
            Assert.Equal(0, LzmaCompressionOptions.MinQuality);
            Assert.Equal(9, LzmaCompressionOptions.MaxQuality);
            Assert.Equal(6, LzmaCompressionOptions.DefaultQuality);
            Assert.Equal(12, LzmaCompressionOptions.MinWindowLog);
            Assert.Equal(30, LzmaCompressionOptions.MaxWindowLog);
            Assert.Equal(23, LzmaCompressionOptions.DefaultWindowLog);
        }
    }
}
