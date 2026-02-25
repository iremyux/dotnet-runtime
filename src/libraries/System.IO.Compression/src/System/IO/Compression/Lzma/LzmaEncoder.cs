// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties to compress data using LZMA/XZ compression.</summary>
    public sealed class LzmaEncoder : IDisposable
    {
        internal SafeLzmaHandle _handle;
        private bool _disposed;

        /// <summary>
        /// True if we finished compressing the entire input.
        /// </summary>
        private bool _finished;

        /// <summary>Initializes a new instance of the <see cref="LzmaEncoder"/> class with default settings.</summary>
        /// <exception cref="IOException">Failed to create the <see cref="LzmaEncoder"/> instance.</exception>
        public LzmaEncoder()
        {
            _disposed = false;
            InitializeEncoder();

            try
            {
                SetQuality(_handle, LzmaUtils.QualityDefault, LzmaChecksum.Crc64);
            }
            catch
            {
                _handle.Dispose();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="LzmaEncoder"/> class with the specified quality level.</summary>
        /// <param name="quality">The compression quality level (0-9, where higher values provide better compression but are slower).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not between 0 and 9.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="LzmaEncoder"/> instance.</exception>
        public LzmaEncoder(int quality)
        {
            _disposed = false;
            InitializeEncoder();

            try
            {
                SetQuality(_handle, quality, LzmaChecksum.Crc64);
            }
            catch
            {
                _handle.Dispose();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="LzmaEncoder"/> class with the specified quality and window size.</summary>
        /// <param name="quality">The compression quality level (0-9, where higher values provide better compression but are slower).</param>
        /// <param name="windowLog">The window size for compression, expressed as base 2 logarithm.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is not between 0 and 9, or <paramref name="windowLog"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="LzmaEncoder"/> instance.</exception>
        public LzmaEncoder(int quality, int windowLog)
        {
            _disposed = false;
            InitializeEncoder();

            try
            {
                SetQualityWithWindowLog(_handle, quality, windowLog, LzmaChecksum.Crc64);
            }
            catch
            {
                _handle.Dispose();
                throw;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="LzmaEncoder"/> class with the specified compression options.</summary>
        /// <param name="compressionOptions">The compression options to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="compressionOptions"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A parameter from <paramref name="compressionOptions"/> is not between the minimum and maximum allowed values.</exception>
        /// <exception cref="IOException">Failed to create the <see cref="LzmaEncoder"/> instance.</exception>
        public LzmaEncoder(LzmaCompressionOptions compressionOptions)
        {
            ArgumentNullException.ThrowIfNull(compressionOptions);

            _disposed = false;
            InitializeEncoder();

            try
            {
                int quality = (int)compressionOptions.GetEffectiveQuality();
                LzmaChecksum checksum = compressionOptions.Checksum;
                int windowLog = compressionOptions.WindowLog;

                if (windowLog != 0)
                {
                    SetQualityWithWindowLog(_handle, quality, windowLog, checksum);
                }
                else
                {
                    SetQuality(_handle, quality, checksum);
                }
            }
            catch
            {
                _handle.Dispose();
                throw;
            }
        }

        [MemberNotNull(nameof(_handle))]
        private void InitializeEncoder()
        {
            _handle = new SafeLzmaHandle();
        }

        internal static void SetQuality(SafeLzmaHandle handle, int quality, LzmaChecksum checksum = LzmaChecksum.Crc64)
        {
            Debug.Assert(handle is not null);

            ValidateQuality(quality);

            unsafe
            {
                LzmaNative.LzmaRetCode ret = Interop.Lzma.lzma_easy_encoder(
                    handle.GetStreamPointer(),
                    (uint)quality,
                    (LzmaNative.LzmaCheck)checksum);

                if (ret != LzmaNative.LzmaRetCode.Ok)
                {
                    LzmaUtils.ThrowForErrorCode(ret);
                }
            }
        }

        internal static void SetQualityWithWindowLog(SafeLzmaHandle handle, int quality, int windowLog, LzmaChecksum checksum = LzmaChecksum.Crc64)
        {
            Debug.Assert(handle is not null);

            ValidateQuality(quality);
            ValidateWindowLog(windowLog);

            unsafe
            {
                LzmaNative.LzmaOptionsLzma options;
                if (Interop.Lzma.lzma_lzma_preset(&options, (uint)quality) != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(quality));
                }

                options.DictSize = (uint)LzmaUtils.WindowSizeFromLog(windowLog);

                LzmaNative.LzmaFilter* filters = stackalloc LzmaNative.LzmaFilter[2];
                filters[0].Id = LzmaNative.FilterLzma2;
                filters[0].Options = &options;
                filters[1].Id = LzmaNative.VliUnknown;
                filters[1].Options = null;

                LzmaNative.LzmaRetCode ret = Interop.Lzma.lzma_stream_encoder(
                    handle.GetStreamPointer(),
                    filters,
                    (LzmaNative.LzmaCheck)checksum);

                if (ret != LzmaNative.LzmaRetCode.Ok)
                {
                    LzmaUtils.ThrowForErrorCode(ret);
                }
            }
        }

        /// <summary>Compresses the specified data.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed from the source.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to the destination.</param>
        /// <param name="isFinalBlock"><see langword="true" /> if this is the final block of data to compress.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        /// <exception cref="IOException">An error occurred during compression.</exception>
        public OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            EnsureNotDisposed();

            bytesConsumed = 0;
            bytesWritten = 0;

            if (_finished)
            {
                return OperationStatus.Done;
            }

            if (source.IsEmpty && !isFinalBlock)
            {
                return OperationStatus.Done;
            }

            return CompressCore(source, destination, out bytesConsumed, out bytesWritten,
                isFinalBlock ? LzmaNative.LzmaAction.Finish : LzmaNative.LzmaAction.Run);
        }

        /// <summary>Flushes any remaining processed data to the destination buffer.</summary>
        /// <param name="destination">The buffer to write the flushed data to.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to the destination.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
        /// <exception cref="IOException">An error occurred during the operation.</exception>
        public OperationStatus Flush(Span<byte> destination, out int bytesWritten)
        {
            EnsureNotDisposed();

            return CompressCore(ReadOnlySpan<byte>.Empty, destination, out _, out bytesWritten, LzmaNative.LzmaAction.SyncFlush);
        }

        private OperationStatus CompressCore(ReadOnlySpan<byte> source, Span<byte> destination,
            out int bytesConsumed, out int bytesWritten, LzmaNative.LzmaAction action)
        {
            bytesConsumed = 0;
            bytesWritten = 0;

            unsafe
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    ref LzmaNative.LzmaStream strm = ref _handle.GetStream();

                    strm.NextIn = inBytes;
                    strm.AvailIn = (nuint)source.Length;
                    strm.NextOut = outBytes;
                    strm.AvailOut = (nuint)destination.Length;

                    nuint inBefore = strm.AvailIn;
                    nuint outBefore = strm.AvailOut;

                    LzmaNative.LzmaRetCode ret = Interop.Lzma.lzma_code(_handle.GetStreamPointer(), action);

                    bytesConsumed = (int)(inBefore - strm.AvailIn);
                    bytesWritten = (int)(outBefore - strm.AvailOut);

                    return ret switch
                    {
                        LzmaNative.LzmaRetCode.Ok => strm.AvailIn == 0 ? OperationStatus.Done : OperationStatus.DestinationTooSmall,
                        LzmaNative.LzmaRetCode.StreamEnd => FinishAndReturnDone(),
                        LzmaNative.LzmaRetCode.BufError => OperationStatus.DestinationTooSmall,
                        LzmaNative.LzmaRetCode.DataError => OperationStatus.InvalidData,
                        _ => ThrowAndReturnInvalid(ret)
                    };
                }
            }
        }

        private OperationStatus FinishAndReturnDone()
        {
            _finished = true;
            return OperationStatus.Done;
        }

        private static OperationStatus ThrowAndReturnInvalid(LzmaNative.LzmaRetCode ret)
        {
            LzmaUtils.ThrowForErrorCode(ret);
            return OperationStatus.InvalidData;
        }

        /// <summary>Gets the maximum compressed size for the specified input length.</summary>
        /// <param name="inputLength">The length of the input data.</param>
        /// <returns>The maximum possible compressed size.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputLength"/> is less than 0 or too large.</exception>
        public static long GetMaxCompressedLength(long inputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(inputLength);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(inputLength, nint.MaxValue);

            ulong result = Interop.Lzma.lzma_stream_buffer_bound((nuint)inputLength);

            // lzma_stream_buffer_bound returns 0 on error (input too large)
            if (result == 0 && inputLength > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputLength), SR.LzmaEncoder_InputLengthTooLarge);
            }

            if (result > long.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(inputLength), SR.LzmaEncoder_InputLengthTooLarge);
            }

            return (long)result;
        }

        /// <summary>Attempts to compress the specified data.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">When this method returns <see langword="true" />, contains the number of bytes written to the destination.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> if the destination buffer is too small.</returns>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return TryCompress(source, destination, out bytesWritten, LzmaUtils.QualityDefault);
        }

        /// <summary>Attempts to compress the specified data with the specified quality.</summary>
        /// <param name="source">The data to compress.</param>
        /// <param name="destination">The buffer to write the compressed data to.</param>
        /// <param name="bytesWritten">When this method returns <see langword="true" />, contains the number of bytes written to the destination.</param>
        /// <param name="quality">The compression quality level (0-9).</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> if the destination buffer is too small.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quality"/> is out of the valid range.</exception>
        public static bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality)
        {
            bytesWritten = 0;
            ValidateQuality(quality);

            unsafe
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    nuint outPos = 0;
                    LzmaNative.LzmaRetCode ret = Interop.Lzma.lzma_easy_buffer_encode(
                        (uint)quality,
                        LzmaNative.LzmaCheck.Crc64,
                        null,
                        inBytes,
                        (nuint)source.Length,
                        outBytes,
                        &outPos,
                        (nuint)destination.Length);

                    bytesWritten = (int)outPos;
                    return ret == LzmaNative.LzmaRetCode.Ok;
                }
            }
        }

        /// <summary>Releases all resources used by the <see cref="LzmaEncoder"/>.</summary>
        public void Dispose()
        {
            _disposed = true;
            _handle?.Dispose();
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(LzmaEncoder));
        }

        private static void ValidateQuality(int quality)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(quality, LzmaUtils.QualityMin, nameof(quality));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, LzmaUtils.QualityMax, nameof(quality));
        }

        private static void ValidateWindowLog(int windowLog)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(windowLog, LzmaUtils.WindowLogMin, nameof(windowLog));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(windowLog, LzmaUtils.WindowLogMax, nameof(windowLog));
        }
    }
}
