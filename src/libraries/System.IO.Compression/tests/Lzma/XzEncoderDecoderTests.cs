// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public class XzEncoderDecoderTests : EncoderDecoderTestBase
    {
        protected override bool SupportsDictionaries => false;
        protected override bool SupportsReset => false;

        protected override int ValidQuality => 3;
        protected override int ValidWindowLog => 18;

        protected override int InvalidQualityTooLow => -1;
        protected override int InvalidQualityTooHigh => 10;
        protected override int InvalidWindowLogTooLow => 11;
        protected override int InvalidWindowLogTooHigh => 31;

        private sealed class XzEncoderAdapter : EncoderAdapter
        {
            private readonly XzEncoder _encoder;

            public XzEncoderAdapter(XzEncoder encoder)
            {
                _encoder = encoder;
            }

            public override OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) =>
                _encoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

            public override OperationStatus Flush(Span<byte> destination, out int bytesWritten) =>
                _encoder.Flush(destination, out bytesWritten);

            public override void Dispose() => _encoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        private sealed class XzDecoderAdapter : DecoderAdapter
        {
            private readonly XzDecoder _decoder;

            public XzDecoderAdapter(XzDecoder decoder)
            {
                _decoder = decoder;
            }

            public override OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
                _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

            public override void Dispose() => _decoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        protected override EncoderAdapter CreateEncoder() =>
            new XzEncoderAdapter(new XzEncoder());

        protected override EncoderAdapter CreateEncoder(int quality, int windowLog) =>
            new XzEncoderAdapter(new XzEncoder(quality, windowLog));

        protected override EncoderAdapter CreateEncoder(DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override DecoderAdapter CreateDecoder() =>
            new XzDecoderAdapter(new XzDecoder());

        protected override DecoderAdapter CreateDecoder(DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override DictionaryAdapter CreateDictionary(ReadOnlySpan<byte> data, int quality) =>
            throw new NotSupportedException();

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            XzEncoder.TryCompress(source, destination, out bytesWritten);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog) =>
            XzEncoder.TryCompress(source, destination, out bytesWritten, quality, windowLog);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            XzDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override long GetMaxCompressedLength(long inputLength) =>
            XzEncoder.GetMaxCompressedLength(inputLength);

        [Fact]
        public void GetMaxCompressedLength_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            long maxValue = (long)Math.Min((ulong)long.MaxValue, (ulong)nuint.MaxValue);

            Assert.Throws<ArgumentOutOfRangeException>("inputLength", () => GetMaxCompressedLength(maxValue));
            Assert.Throws<ArgumentOutOfRangeException>("inputLength", () => GetMaxCompressedLength(maxValue + 1L));
            Assert.Throws<ArgumentOutOfRangeException>("inputLength", () => GetMaxCompressedLength(-1));
        }

        [Fact]
        public void Encoder_WithCompressionOptions_Roundtrips()
        {
            XzCompressionOptions options = new()
            {
                Quality = 3,
                WindowLog = 18,
                Checksum = XzChecksumType.Crc32
            };

            byte[] input = XzTestUtils.CreateTestData(5000);
            byte[] compressed = new byte[XzEncoder.GetMaxCompressedLength(input.Length)];
            byte[] decompressed = new byte[input.Length];

            using XzEncoder encoder = new(options);
            OperationStatus result = encoder.Compress(input, compressed, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(input.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);

            Assert.True(XzDecoder.TryDecompress(compressed.AsSpan(0, bytesWritten), decompressed, out int decompressedLength));
            Assert.Equal(input.Length, decompressedLength);
            Assert.Equal(input, decompressed);
        }

        [Theory]
        [InlineData(XzChecksumType.None)]
        [InlineData(XzChecksumType.Crc32)]
        [InlineData(XzChecksumType.Crc64)]
        [InlineData(XzChecksumType.Sha256)]
        public void Encoder_ChecksumType_Roundtrips(XzChecksumType checksumType)
        {
            XzCompressionOptions options = new()
            {
                Quality = 3,
                Checksum = checksumType
            };

            byte[] input = XzTestUtils.CreateTestData(1000);
            byte[] compressed = new byte[XzEncoder.GetMaxCompressedLength(input.Length)];
            byte[] decompressed = new byte[input.Length];

            using XzEncoder encoder = new(options);
            OperationStatus result = encoder.Compress(input, compressed, out _, out int bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, result);

            Assert.True(XzDecoder.TryDecompress(compressed.AsSpan(0, bytesWritten), decompressed, out int decompressedLength));
            Assert.Equal(input.Length, decompressedLength);
            Assert.Equal(input, decompressed);
        }

        [Fact]
        public void Encoder_EnableExtremeMode_Roundtrips()
        {
            XzCompressionOptions options = new()
            {
                Quality = 3,
                EnableExtremeMode = true
            };

            byte[] input = XzTestUtils.CreateTestData(5000);
            byte[] compressed = new byte[XzEncoder.GetMaxCompressedLength(input.Length)];
            byte[] decompressed = new byte[input.Length];

            using XzEncoder encoder = new(options);
            OperationStatus result = encoder.Compress(input, compressed, out _, out int bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, result);

            Assert.True(XzDecoder.TryDecompress(compressed.AsSpan(0, bytesWritten), decompressed, out int decompressedLength));
            Assert.Equal(input.Length, decompressedLength);
            Assert.Equal(input, decompressed);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(6)]
        [InlineData(9)]
        public void TryCompress_WithQuality_Roundtrips(int quality)
        {
            byte[] input = XzTestUtils.CreateTestData(1000);
            byte[] compressed = new byte[XzEncoder.GetMaxCompressedLength(input.Length)];
            byte[] decompressed = new byte[input.Length];

            Assert.True(XzEncoder.TryCompress(input, compressed, out int bytesWritten, quality));
            Assert.True(XzDecoder.TryDecompress(compressed.AsSpan(0, bytesWritten), decompressed, out int decompressedLength));
            Assert.Equal(input.Length, decompressedLength);
            Assert.Equal(input, decompressed);
        }

        [Fact]
        public void Decoder_MaxWindowLog_InvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>("maxWindowLog", () => new XzDecoder(maxWindowLog: 11));
            Assert.Throws<ArgumentOutOfRangeException>("maxWindowLog", () => new XzDecoder(maxWindowLog: 31));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(10)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void TryCompress_InvalidQuality_Static(int quality)
        {
            byte[] input = XzTestUtils.CreateTestData(100);
            byte[] output = new byte[XzEncoder.GetMaxCompressedLength(input.Length)];

            Assert.Throws<ArgumentOutOfRangeException>("quality", () => XzEncoder.TryCompress(input, output, out _, quality));
        }
    }
}
