// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLzmaHandle : SafeHandle
    {
        public SafeLzmaHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Lzma.lzma_end((LzmaNative.LzmaStream*)handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}