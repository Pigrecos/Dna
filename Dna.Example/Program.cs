﻿using Dna.Binary.Windows;
using Dna.ControlFlow;
using Dna.ControlFlow.Analysis;
using Dna.Emulation;
using Dna.Lifting;
using Dna.Optimization;
using Dna.Optimization.Passes;
using Dna.Relocation;
using Dna.Synthesis.Jit;
using Dna.Synthesis.Miasm;
using Dna.Synthesis.Parsing;
using Dna.Synthesis.Simplification;
using Dna.Synthesis.Utils;
using DotNetGraph.Extensions;
using Rivers;
using Rivers.Analysis;
using System.Diagnostics;
using Grpc.Net.Client;
using TritonTranslator.Arch;
using TritonTranslator.Arch.X86;
using ClangSharp.Interop;
using ClangSharp;
using Dna.Decompiler;
using Dna.Emulation.Unicorn;
using Dna.Decompilation;
using Dna.Structuring.Stackify;
using Dna.Emulation.Symbolic;
using TritonTranslator.Intermediate;
using System;
using TritonTranslator.Conversion;
using LLVMSharp.Interop;
using Dna.Decompiler.Rellic;
using Dna.LLVMInterop;
using System.Runtime.InteropServices;
using Dna.LLVMInterop.API.RegionAnalysis.Wrapper;
using static Dna.LLVMInterop.NativeOptimizationApi;
using Dna.LLVMInterop.API.Optimization;
using Dna.LLVMInterop.Passes;
using Dna.Utilities;
using Dna.Devirtualization;
using Dna.Extensions;
using Dna.Structuring.Stacker;
using Dna.LLVMInterop.API.LLVMBindings.Transforms;
using Dna.LLVMInterop.API.LLVMBindings.IR;
using Dna.LLVMInterop.API.LLVMBindings.Transforms.IPO;
using static Dna.LLVMInterop.NativePassApi;

// Load the 64 bit PE file.
// Note: This file is automatically copied to the build directory.
var path = @"SampleExecutable.bin";
var binary = new WindowsBinary(64, File.ReadAllBytes(path), 0x140000000);

/*
// Replace themida spinlock with nop.
binary.WriteBytes(0x000000014001552B, new byte[]
{
    0x90, 0x90, 0x90, 0x90, 0x90, 0x90,
    0x90, 0x90,
    0x90, 0x90, 0x90, 0x90, 0x90
});
*/
/*
var assembler = new Iced.Intel.Assembler(64);
assembler.jmp(0x14004B2EE);

var encoded = InstructionRelocator.EncodeInstructions(assembler.Instructions.ToList(), 0x140015C47, out ulong endRIP);
binary.WriteBytes(0x140015C47, encoded);
*/

// Instantiate dna.
var dna = new Dna.Dna(binary);

// Parse a (virtualized) control flow graph from the binary.
ulong funcAddr = 0x1400012E4;
var cfg = dna.RecursiveDescent.ReconstructCfg(funcAddr);

// Instantiate the cpu architecture.
var architecture = new X86CpuArchitecture(ArchitectureId.ARCH_X86_64);

bool explore = false;
if (explore)
{
    var cfgExplorer = new CfgExplorer(dna, architecture);
    cfgExplorer.DevirtualizeRoutine(funcAddr);
}

// Instantiate a class for lifting control flow graphs to our intermediate language.
var cfgLifter = new CfgLifter(architecture);

// Lift the control flow graph to TTIR.
var liftedCfg = cfgLifter.LiftCfg(cfg);

for (int i = 0; i < 3; i++)
    Console.WriteLine("");

var llvmLifter = new LLVMLifter(architecture);


// beforecustompipeline.ll
var ctx = LLVMContextRef.Create();
// -passes=sccp,sroa,dce,dse,adce,licm,gvn,newgvn -memdep-block-scan-limit=1000000000 -gvn-max-num-deps=25000000
//var memBuffer = LlvmUtilities.CreateMemoryBuffer(@"C:\Users\colton\source\repos\Dna\Dna.Example\bin\x64\Debug\net7.0\cant_resolve.ll");
//ctx.TryParseIR(memBuffer, out LLVMModuleRef unicornTraceModule, out string unicornLoadMsg);

llvmLifter.Lift(liftedCfg);
//llvmLifter.module = unicornTraceModule;
//llvmLifter.llvmFunction = llvmLifter.Module.FirstFunction;

/*
LlvmUtilities.LLVMParseCommandLineOptions(new string[] { "-memdep-block-scan-limit=10000000",
    "-memdep-block-scan-limit=10000000",
    "-dse-memoryssa-defs-per-block-limit=1000000",
    "-dse-memoryssa-partial-store-limit=1000000",
    "-dse-memoryssa-path-check-limit=1000000",
    "-dse-memoryssa-scanlimit=1000000",
    "-dse-memoryssa-walklimit=1000000",
    "-dse-memoryssa-otherbb-cost=2",
    "-memssa-check-limit=1000000",
    "memdep-block-number-limit=10000",
    "-memdep-block-scan-limit=1000000",
    "-gvn-max-block-speculations=1000000",
    "-gvn-max-num-deps=1000000",
});
*/

// Lift the cfg to LLVM IR.
//llvmLifter.Lift(liftedCfg);

// Optimize the routine.
bool optimize = false;
if (optimize)
{
    var passManager2 = llvmLifter.Module.CreateFunctionPassManager();
    passManager2.AddBasicAliasAnalysisPass();
    passManager2.AddTypeBasedAliasAnalysisPass();
    passManager2.AddScopedNoAliasAAPass();
    passManager2.AddLowerExpectIntrinsicPass();
    passManager2.AddCFGSimplificationPass();
    passManager2.AddPromoteMemoryToRegisterPass();
    passManager2.AddEarlyCSEPass();
    passManager2.AddDCEPass();
    passManager2.AddAggressiveDCEPass();
    passManager2.AddDeadStoreEliminationPass();
    passManager2.AddInstructionCombiningPass();
    passManager2.AddCFGSimplificationPass();
    passManager2.AddDeadStoreEliminationPass();
    passManager2.AddAggressiveDCEPass();
    passManager2.InitializeFunctionPassManager();
    for (int i = 0; i < 5; i++)
    {
        passManager2.RunFunctionPassManager(llvmLifter.llvmFunction);
    }

    passManager2.FinalizeFunctionPassManager();
}

var readBytes = (ulong address, uint size) =>
{
    var bytes = binary.ReadBytes(address, (int)size);
    var value = size switch
    {
        1 => bytes[0],
        2 => BitConverter.ToUInt16(bytes),
        4 => BitConverter.ToUInt32(bytes),
        8 => BitConverter.ToUInt64(bytes),
        _ => throw new InvalidOperationException()
    };
    return (ulong)value;
};


var dgReadBytes = new dgReadBinaryContents(readBytes);
var ptrReadBinaryContents = Marshal.GetFunctionPointerForDelegate(dgReadBytes);

var ptrAlias = ClassifyingAliasAnalysisPass.PtrGetAliasResult;

var llPath = @"optimized_vm_entry2.ll";



// Run the standard O3 pipeline.
for (int i = 0; i < 5; i++)
{
    OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, false, 0, false, 0, false);
}

// Run the O3 pipeline with custom alias analysis.
OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false);

// Run the O3 pipeline with ptr alias analysis AND aggressive loop unrolling enabled.
OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, true, true, ptrAlias, false, 0, false);



// Run the O3 pipeline one last time with custom alias analysis.
llvmLifter.Module.PrintToFile("foo2.ll");
OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false);

//llvmLifter.Module.PrintToFile(llPath);
//var myPass = new ConstantConcretizationPass(llvmLifter.llvmFunction, llvmLifter.builder, binary);
//myPass.Execute();

// Run the O3 pipeline one last time with custom alias analysis.

PointerClassifier.Seen.Clear();
PointerClassifier.print = true;
for (int i = 0; i < 10; i++)
{
    Console.WriteLine(i);
    OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false);


    if (i % 4 == 0)
    {

        var pass2 = new ConstantConcretizationPass(llvmLifter.llvmFunction, llvmLifter.builder, binary);
        //pass2.Execute();

        // ClassifyingAliasAnalysisPass.print = true;
    }

    llvmLifter.Module.PrintToFile("foo.ll");
    Console.WriteLine(i.ToString());
    OptimizationApi.OptimizeModule(llvmLifter.Module, llvmLifter.llvmFunction, false, true, ptrAlias, false, 0, false, false);
    Console.WriteLine("foo foo foo");
}


llvmLifter.Module.PrintToFile(llPath);

var fpm = new FunctionPassManager();
var pmb = new PassManagerBuilder();
var moduleManager = new PassManager();

// Create a reducible and sensible control flow graph.
fpm.Add(ScalarPasses.CreateCFGSimplificationPass());
fpm.Add(PassApi.CreateControlledNodeSplittingPass());
fpm.Add(ScalarPasses.CreateCFGSimplificationPass());
fpm.Add(PassApi.CreateUnSwitchPass());
fpm.Add(ScalarPasses.CreateLoopSimplifyCFGPass());
fpm.Add(PassApi.CreateLoopExitEnumerationPass());
fpm.Add(PassApi.CreateUnSwitchPass());
fpm.Add(PassApi.CreateControlledNodeSplittingPass());

// Structure the CFG.
var cfPass = new ControlFlowStructuringPass();
var nativeCfPass = PassApi.CreateControlFlowStructuringPass(cfPass.PtrStructureFunction);
fpm.Add(nativeCfPass);
pmb.PopulateFunctionPassManager(fpm);
pmb.PopulateModulePassManager(moduleManager);

fpm.DoInitialization();

fpm.Run(llvmLifter.llvmFunction);

fpm.DoFinalization();

moduleManager.Run(llvmLifter.llvmFunction.GlobalParent);


ControlFlowGraph<LLVMValueRef> llvmGraph = new ControlFlowGraph<LLVMValueRef>(0);
foreach(var llvmBlock in llvmLifter.llvmFunction.BasicBlocks)
{
    // Allocate a new block.
    var blk = llvmGraph.CreateBlock((ulong)llvmBlock.Handle);

    // Copy the instructions.
    blk.Instructions.AddRange(llvmBlock.GetInstructions());
}

foreach(var block in llvmGraph.GetBlocks())
{
    var exitInstruction = block.ExitInstruction;
   // if (exitInstruction.ToString().Contains("cond120131"))
      //  Debugger.Break();
    var operands = exitInstruction.GetOperands().ToList();
    foreach(var operand in operands)
    {
        Console.WriteLine(operand.ToString());
        if (operand.Kind != LLVMValueKind.LLVMBasicBlockValueKind)
            continue;

        var outgoingBlk = llvmGraph.GetBlocks().Single(x => x.Address == (ulong)operand.Handle);
        block.AddOutgoingEdge(new BlockEdge<LLVMValueRef>(block, outgoingBlk));
    }


    if (exitInstruction.ToString().Contains("cond120131"))
    {
        var op0 = block.ExitInstruction.GetOperand(1);
        var op1 = block.ExitInstruction.GetOperand(2);
        Console.WriteLine(exitInstruction);
        Console.WriteLine(op0);
        Console.WriteLine(op1);
        var edge1 = block.GetOutgoingEdges().Single(x => x.TargetBlock.Name == block.ExitInstruction.GetOperand(1).Handle.ToString("X"));
        var edge2 = block.GetOutgoingEdges().Single(x => x.TargetBlock.Name == block.ExitInstruction.GetOperand(2).Handle.ToString("X"));
        Console.WriteLine("why.");
      //  Debugger.Break();
    }
        Console.WriteLine(exitInstruction);
}

llvmLifter.llvmFunction.GlobalParent.PrintToFile("whyisthiscrashing.ll");
var dotGraph = GraphVisualizer.GetDotGraph(llvmGraph);
var dot = dotGraph.Compile();
File.WriteAllText("llvmGraph.dot", dot);

var domTree = new ImmutableDomTree<LLVMValueRef>(llvmGraph);
var loopAnalysis = new LoopAnalysis<LLVMValueRef>(llvmGraph, domTree);

var structurer = new StackingStructurer(llvmGraph, domTree);
structurer.Structure();

bool compile = true;
if (compile)
{
    // Compile to a .exe using clang.
    Console.WriteLine("Compiling to an exe.");
    var compiledPath = ClangCompiler.Compile(llPath);

    Console.WriteLine("Loading into IDA.");
    var exePath = IDALoader.Load(compiledPath);
    Console.WriteLine("Loaded executable into IDA.");
}

Debugger.Break();