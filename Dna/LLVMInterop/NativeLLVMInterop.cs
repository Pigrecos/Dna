﻿using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop
{
    public static class NativeLLVMInterop
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong dgReadBinaryContents(ulong rva, uint size);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern IntPtr OptimizeModule(LLVMOpaqueModule* M, IntPtr readBinaryContents);
    }
}