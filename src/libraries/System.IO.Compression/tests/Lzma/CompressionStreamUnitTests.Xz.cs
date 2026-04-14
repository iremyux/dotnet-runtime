// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class XzStreamUnitTests : CompressionStreamUnitTestBase
    {
        private static readonly ConcurrentDictionary<string, string> s_compressedFiles = new();

        public override Stream CreateStream(Stream stream, CompressionMode mode) => new XzStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new XzStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new XzStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new XzStream(stream, level, leaveOpen);
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) =>
            new XzStream(stream, options is null ? null! : new XzCompressionOptions { Quality = options.CompressionLevel }, leaveOpen);

        public override Stream BaseStream(Stream stream) => ((XzStream)stream).BaseStream;

        public override int BufferSize => 1 << 16;

        protected override string CompressedTestFile(string uncompressedPath)
        {
            return s_compressedFiles.GetOrAdd(uncompressedPath, static path =>
            {
                string compressedPath = Path.Combine(Path.GetTempPath(), "XzTestData", Path.GetFileName(path) + ".xz");
                Directory.CreateDirectory(Path.GetDirectoryName(compressedPath)!);

                if (!File.Exists(compressedPath))
                {
                    byte[] uncompressedData = File.ReadAllBytes(path);
                    using FileStream fs = File.Create(compressedPath);
                    using XzStream compressor = new(fs, CompressionLevel.Optimal);
                    compressor.Write(uncompressedData);
                }

                return compressedPath;
            });
        }

        [Fact]
        public void XzStream_DecompressInvalidData_InvalidDataException()
        {
            byte[] invalidCompressedData = [0x01, 0x02, 0x03, 0x04, 0x05];
            using MemoryStream input = new(invalidCompressedData);
            using XzStream decompressionStream = new(input, CompressionMode.Decompress);
            byte[] buffer = new byte[16];

            Assert.Throws<InvalidDataException>(() => decompressionStream.Read(buffer, 0, buffer.Length));
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.NoCompression)]
        [InlineData(CompressionLevel.SmallestSize)]
        public void XzStream_CompressionLevel_Roundtrips(CompressionLevel level)
        {
            byte[] testData = XzTestUtils.CreateTestData(5000);
            using MemoryStream compressed = new();

            using (XzStream compressor = new(compressed, level, leaveOpen: true))
            {
                compressor.Write(testData);
            }

            Assert.True(compressed.Length > 0);

            compressed.Position = 0;
            using MemoryStream decompressed = new();
            using (XzStream decompressor = new(compressed, CompressionMode.Decompress))
            {
                decompressor.CopyTo(decompressed);
            }

            Assert.Equal(testData, decompressed.ToArray());
        }

        [Fact]
        public void XzStream_WithCompressionOptions_Roundtrips()
        {
            XzCompressionOptions options = new()
            {
                Quality = 3,
                WindowLog = 18,
                Checksum = XzChecksumType.Crc32
            };

            byte[] testData = XzTestUtils.CreateTestData(5000);
            using MemoryStream compressed = new();

            using (XzStream compressor = new(compressed, options, leaveOpen: true))
            {
                compressor.Write(testData);
            }

            Assert.True(compressed.Length > 0);

            compressed.Position = 0;
            using MemoryStream decompressed = new();
            using (XzStream decompressor = new(compressed, CompressionMode.Decompress))
            {
                decompressor.CopyTo(decompressed);
            }

            Assert.Equal(testData, decompressed.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task XzStream_Roundtrip_Async(bool leaveOpen)
        {
            byte[] testData = XzTestUtils.CreateTestData(5000);
            using MemoryStream compressed = new();

            await using (XzStream compressor = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                await compressor.WriteAsync(testData);
            }

            compressed.Position = 0;
            using MemoryStream decompressed = new();
            await using (XzStream decompressor = new(compressed, CompressionMode.Decompress, leaveOpen))
            {
                await decompressor.CopyToAsync(decompressed);
            }

            Assert.Equal(testData, decompressed.ToArray());
        }

        [Fact]
        public void XzStream_NullOptions_ThrowsArgumentNullException()
        {
            using MemoryStream ms = new();
            Assert.Throws<ArgumentNullException>("compressionOptions", () => new XzStream(ms, (XzCompressionOptions)null!));
        }

        [Fact]
        public void XzStream_EnableExtremeMode_Roundtrips()
        {
            XzCompressionOptions options = new()
            {
                Quality = 3,
                EnableExtremeMode = true
            };

            byte[] testData = XzTestUtils.CreateTestData(5000);
            using MemoryStream compressed = new();

            using (XzStream compressor = new(compressed, options, leaveOpen: true))
            {
                compressor.Write(testData);
            }

            compressed.Position = 0;
            using MemoryStream decompressed = new();
            using (XzStream decompressor = new(compressed, CompressionMode.Decompress))
            {
                decompressor.CopyTo(decompressed);
            }

            Assert.Equal(testData, decompressed.ToArray());
        }
    }
}
