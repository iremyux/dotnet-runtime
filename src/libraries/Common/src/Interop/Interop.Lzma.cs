// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Lzma
    {
        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial LzmaNative.LzmaRetCode lzma_code(LzmaNative.LzmaStream* strm, LzmaNative.LzmaAction action);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial void lzma_end(LzmaNative.LzmaStream* strm);
        
        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial LzmaNative.LzmaRetCode lzma_easy_encoder(LzmaNative.LzmaStream* strm, uint preset, LzmaNative.LzmaCheck check);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial LzmaNative.LzmaRetCode lzma_stream_encoder(LzmaNative.LzmaStream* strm, LzmaNative.LzmaFilter* filters, LzmaNative.LzmaCheck check);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial LzmaNative.LzmaRetCode lzma_easy_buffer_encode(uint preset, LzmaNative.LzmaCheck check, void* allocator, byte* inBuf, nuint inSize, byte* outBuf, nuint* outPos, nuint outSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial ulong lzma_stream_buffer_bound(nuint uncompressedSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial LzmaNative.LzmaRetCode lzma_stream_decoder(LzmaNative.LzmaStream* strm, ulong memlimit, uint flags); 

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial LzmaNative.LzmaRetCode lzma_auto_decoder(LzmaNative.LzmaStream* strm, ulong memlimit, uint flags);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial LzmaNative.LzmaRetCode lzma_stream_buffer_decode(ulong* memlimit, uint flags, void* allocator, byte* inBuf, nuint* inPos, nuint inSize, byte* outBuf, nuint* outPos, nuint outSize);
    }
}