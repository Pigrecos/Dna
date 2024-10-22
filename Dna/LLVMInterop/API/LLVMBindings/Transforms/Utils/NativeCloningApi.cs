﻿using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WebAssembly.Instructions;

namespace Dna.LLVMInterop.API.LLVMBindings.Transforms.Utils
{
    // for LLVM ver.18.1.7
    public enum AttrKind : uint
    {
        FirstEnumAttr = 1,
        AllocAlign = 1,
        AllocatedPointer = 2,
        AlwaysInline = 3,
        Builtin = 4,
        Cold = 5,
        Convergent = 6,
        CoroDestroyOnlyWhenComplete = 7,
        DeadOnUnwind = 8,
        DisableSanitizerInstrumentation = 9,
        FnRetThunkExtern = 10,
        Hot = 11,
        ImmArg = 12,
        InReg = 13,
        InlineHint = 14,
        JumpTable = 15,
        MinSize = 16,
        MustProgress = 17,
        Naked = 18,
        Nest = 19,
        NoAlias = 20,
        NoBuiltin = 21,
        NoCallback = 22,
        NoCapture = 23,
        NoCfCheck = 24,
        NoDuplicate = 25,
        NoFree = 26,
        NoImplicitFloat = 27,
        NoInline = 28,
        NoMerge = 29,
        NoProfile = 30,
        NoRecurse = 31,
        NoRedZone = 32,
        NoReturn = 33,
        NoSanitizeBounds = 34,
        NoSanitizeCoverage = 35,
        NoSync = 36,
        NoUndef = 37,
        NoUnwind = 38,
        NonLazyBind = 39,
        NonNull = 40,
        NullPointerIsValid = 41,
        OptForFuzzing = 42,
        OptimizeForDebugging = 43,
        OptimizeForSize = 44,
        OptimizeNone = 45,
        PresplitCoroutine = 46,
        ReadNone = 47,
        ReadOnly = 48,
        Returned = 49,
        ReturnsTwice = 50,
        SExt = 51,
        SafeStack = 52,
        SanitizeAddress = 53,
        SanitizeHWAddress = 54,
        SanitizeMemTag = 55,
        SanitizeMemory = 56,
        SanitizeThread = 57,
        ShadowCallStack = 58,
        SkipProfile = 59,
        Speculatable = 60,
        SpeculativeLoadHardening = 61,
        StackProtect = 62,
        StackProtectReq = 63,
        StackProtectStrong = 64,
        StrictFP = 65,
        SwiftAsync = 66,
        SwiftError = 67,
        SwiftSelf = 68,
        WillReturn = 69,
        Writable = 70,
        WriteOnly = 71,
        ZExt = 72,
        LastEnumAttr = 72,
        FirstTypeAttr = 73,
        ByRef = 73,
        ByVal = 74,
        ElementType = 75,
        InAlloca = 76,
        Preallocated = 77,
        StructRet = 78,
        LastTypeAttr = 78,
        FirstIntAttr = 79,
        Alignment = 79,
        AllocKind = 80,
        AllocSize = 81,
        Dereferenceable = 82,
        DereferenceableOrNull = 83,
        Memory = 84,
        NoFPClass = 85,
        StackAlignment = 86,
        UWTable = 87,
        VScaleRange = 88,
        LastIntAttr = 88,
    }

    public static class NativeCloningApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern sbyte* InlineFunction(LLVMOpaqueValue* callInst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void AddParamAttr(LLVMOpaqueValue* function, uint index, AttrKind kind);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void MakeMustTail(LLVMOpaqueValue* callInst);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void MakeDsoLocal(LLVMOpaqueValue* function, bool dsoLocal);
    }
}
