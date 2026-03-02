// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class LzmaStreamUnitTests : CompressionStreamUnitTestBase
    {
        private static readonly ConcurrentDictionary<string, string> s_compressedFiles = new();

        public override Stream CreateStream(Stream stream, CompressionMode mode) => new LzmaStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new LzmaStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new LzmaStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new LzmaStream(stream, level, leaveOpen);
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) =>
            new LzmaStream(stream, options is null ? null! : new LzmaCompressionOptions { Quality = options.CompressionLevel }, leaveOpen);

        public override Stream BaseStream(Stream stream) => ((LzmaStream)stream).BaseStream;

        public override int BufferSize => 1 << 16;

        protected override string CompressedTestFile(string uncompressedPath)
        {
            return s_compressedFiles.GetOrAdd(uncompressedPath, static path =>
            {
                string compressedPath = Path.Combine(Path.GetTempPath(), "LzmaTestData", Path.GetFileName(path) + ".xz");
                Directory.CreateDirectory(Path.GetDirectoryName(compressedPath)!);

                if (!File.Exists(compressedPath))
                {
                    byte[] uncompressedData = File.ReadAllBytes(path);
                    using FileStream fs = File.Create(compressedPath);
                    using LzmaStream compressor = new(fs, CompressionLevel.Optimal);
                    compressor.Write(uncompressedData);
                }

                return compressedPath;
            });
        }

        [Fact]
        public void LzmaStream_DecompressInvalidData_InvalidDataException()
        {
            byte[] invalidCompressedData = [0x01, 0x02, 0x03, 0x04, 0x05];
            using MemoryStream input = new(invalidCompressedData);
            using LzmaStream decompressionStream = new(input, CompressionMode.Decompress);
            byte[] buffer = new byte[16];

            Assert.Throws<InvalidDataException>(() => decompressionStream.Read(buffer, 0, buffer.Length));
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.NoCompression)]
        [InlineData(CompressionLevel.SmallestSize)]
        public void LzmaStream_CompressionLevel_Roundtrips(CompressionLevel level)
        {
            byte[] testData = LzmaTestUtils.CreateTestData(5000);
            using MemoryStream compressed = new();

            using (LzmaStream compressor = new(compressed, level, leaveOpen: true))
            {
                compressor.Write(testData);
            }

            Assert.True(compressed.Length > 0);

            compressed.Position = 0;
            using MemoryStream decompressed = new();
            using (LzmaStream decompressor = new(compressed, CompressionMode.Decompress))
            {
                decompressor.CopyTo(decompressed);
            }

            Assert.Equal(testData, decompressed.ToArray());
        }

        [Fact]
        public void LzmaStream_WithCompressionOptions_Roundtrips()
        {
            LzmaCompressionOptions options = new()
            {
                Quality = 3,
                WindowLog = 18,
                Checksum = LzmaChecksumType.Crc32
            };

            byte[] testData = LzmaTestUtils.CreateTestData(5000);
            using MemoryStream compressed = new();

            using (LzmaStream compressor = new(compressed, options, leaveOpen: true))
            {
                compressor.Write(testData);
            }

            Assert.True(compressed.Length > 0);

            compressed.Position = 0;
            using MemoryStream decompressed = new();
            using (LzmaStream decompressor = new(compressed, CompressionMode.Decompress))
            {
                decompressor.CopyTo(decompressed);
            }

            Assert.Equal(testData, decompressed.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LzmaStream_Roundtrip_Async(bool leaveOpen)
        {
            byte[] testData = LzmaTestUtils.CreateTestData(5000);
            using MemoryStream compressed = new();

            await using (LzmaStream compressor = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                await compressor.WriteAsync(testData);
            }

            compressed.Position = 0;
            using MemoryStream decompressed = new();
            await using (LzmaStream decompressor = new(compressed, CompressionMode.Decompress, leaveOpen))
            {
                await decompressor.CopyToAsync(decompressed);
            }

            Assert.Equal(testData, decompressed.ToArray());
        }

        [Fact]
        public void LzmaStream_NullOptions_ThrowsArgumentNullException()
        {
            using MemoryStream ms = new();
            Assert.Throws<ArgumentNullException>("compressionOptions", () => new LzmaStream(ms, (LzmaCompressionOptions)null!));
        }

        [Fact]
        public void LzmaStream_EnableExtremeMode_Roundtrips()
        {
            LzmaCompressionOptions options = new()
            {
                Quality = 3,
                EnableExtremeMode = true
            };

            byte[] testData = LzmaTestUtils.CreateTestData(5000);
            using MemoryStream compressed = new();

            using (LzmaStream compressor = new(compressed, options, leaveOpen: true))
            {
                compressor.Write(testData);
            }

            compressed.Position = 0;
            using MemoryStream decompressed = new();
            using (LzmaStream decompressor = new(compressed, CompressionMode.Decompress))
            {
                decompressor.CopyTo(decompressed);
            }

            Assert.Equal(testData, decompressed.ToArray());
        }
    }
}
