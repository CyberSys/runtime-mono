// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    internal static partial class FunctionWrapper
    {
        private static IntPtr LoadFunctionPointer(IntPtr nativeLibraryHandle, string functionName)
            => Interop.Kernel32.GetProcAddress(nativeLibraryHandle, functionName);
    }
}
