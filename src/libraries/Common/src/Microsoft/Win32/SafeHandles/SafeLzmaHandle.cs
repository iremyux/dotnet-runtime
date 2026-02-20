// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLzmaHandle : SafeHandle
    {
        private LzmaNative.LzmaStream _stream;

        public SafeLzmaHandle() : base(IntPtr.Zero, true)
        {
            // Initialize the stream to zero
            _stream = default;
            // Use 1 as a sentinel value to indicate the handle is valid
            // The actual state is in _stream
            SetHandle((IntPtr)1);
        }

        protected override bool ReleaseHandle()
        {
            unsafe
            {
                fixed (LzmaNative.LzmaStream* strm = &_stream)
                {
                    Interop.Lzma.lzma_end(strm);
                }
            }
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <summary>
        /// Gets a reference to the underlying LzmaStream structure.
        /// </summary>
        public ref LzmaNative.LzmaStream GetStream() => ref _stream;

        /// <summary>
        /// Gets an unsafe pointer to the LzmaStream structure.
        /// </summary>
        public unsafe LzmaNative.LzmaStream* GetStreamPointer()
        {
            return (LzmaNative.LzmaStream*)Unsafe.AsPointer(ref _stream);
        }
    }
}
