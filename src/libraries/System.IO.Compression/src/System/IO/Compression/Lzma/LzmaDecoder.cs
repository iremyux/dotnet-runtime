// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties to decompress data using LZMA/XZ decompression.</summary>
    public sealed class LzmaDecoder : IDisposable
    {
        internal SafeLzmaHandle _handle;
        private bool _disposed;
        private bool _finished;

        /// <summary>Initializes a new instance of the <see cref="LzmaDecoder"/> class with default settings.</summary>
        /// <exception cref="IOException">Failed to create the <see cref="LzmaDecoder"/> instance.</exception>
        public LzmaDecoder()
        {
            _disposed = false;
            _finished = false;
            InitializeDecoder();
        }

        [MemberNotNull(nameof(_handle))]
        private void InitializeDecoder()
        {
            _handle = new SafeLzmaHandle();

            unsafe
            {
                // Use auto_decoder to support both XZ and legacy LZMA formats
                // Use UINT64_MAX for memlimit to allow any stream
                LzmaNative.LzmaRetCode ret = Interop.Lzma.lzma_auto_decoder(
                    _handle.GetStreamPointer(),
                    ulong.MaxValue,
                    0);

                if (ret != LzmaNative.LzmaRetCode.Ok)
                {
                    _handle.Dispose();
                    LzmaUtils.ThrowForErrorCode(ret);
                }
            }
        }

        /// <summary>Decompresses the specified data.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed from the source.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to the destination.</param>
        /// <returns>An <see cref="OperationStatus"/> indicating the result of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
        /// <exception cref="IOException">An error occurred during decompression.</exception>
        public OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
        {
            bytesConsumed = 0;
            bytesWritten = 0;

            if (_finished)
            {
                return OperationStatus.Done;
            }

            EnsureNotDisposed();

            if (destination.IsEmpty)
            {
                return OperationStatus.DestinationTooSmall;
            }

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

                    LzmaNative.LzmaRetCode ret = Interop.Lzma.lzma_code(_handle.GetStreamPointer(), LzmaNative.LzmaAction.Run);

                    bytesConsumed = (int)(inBefore - strm.AvailIn);
                    bytesWritten = (int)(outBefore - strm.AvailOut);

                    return ret switch
                    {
                        LzmaNative.LzmaRetCode.Ok when strm.AvailOut == 0 => OperationStatus.DestinationTooSmall,
                        LzmaNative.LzmaRetCode.Ok => OperationStatus.NeedMoreData,
                        LzmaNative.LzmaRetCode.StreamEnd => FinishAndReturnDone(),
                        LzmaNative.LzmaRetCode.BufError => OperationStatus.DestinationTooSmall,
                        LzmaNative.LzmaRetCode.DataError or LzmaNative.LzmaRetCode.FormatError => OperationStatus.InvalidData,
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

        /// <summary>Attempts to decompress the specified data.</summary>
        /// <param name="source">The compressed data to decompress.</param>
        /// <param name="destination">The buffer to write the decompressed data to.</param>
        /// <param name="bytesWritten">When this method returns <see langword="true" />, contains the number of bytes written to the destination.</param>
        /// <returns><see langword="true" /> on success; <see langword="false" /> otherwise.</returns>
        /// <remarks>If this method returns <see langword="false" />, <paramref name="destination" /> may be empty or contain partially decompressed data, and <paramref name="bytesWritten" /> might be zero or greater than zero but less than the expected total.</remarks>
        public static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;

            if (source.IsEmpty)
            {
                return false;
            }

            unsafe
            {
                fixed (byte* inBytes = &MemoryMarshal.GetReference(source))
                fixed (byte* outBytes = &MemoryMarshal.GetReference(destination))
                {
                    ulong memlimit = ulong.MaxValue;
                    nuint inPos = 0;
                    nuint outPos = 0;

                    LzmaNative.LzmaRetCode ret = Interop.Lzma.lzma_stream_buffer_decode(
                        &memlimit,
                        0,
                        null,
                        inBytes,
                        &inPos,
                        (nuint)source.Length,
                        outBytes,
                        &outPos,
                        (nuint)destination.Length);

                    bytesWritten = (int)outPos;
                    return ret == LzmaNative.LzmaRetCode.Ok;
                }
            }
        }

        /// <summary>Releases all resources used by the <see cref="LzmaDecoder"/>.</summary>
        public void Dispose()
        {
            _disposed = true;
            _handle?.Dispose();
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(LzmaDecoder));
        }
    }
}
