cmake_minimum_required(VERSION 3.10)
project(LLVMInterOp)

project(LLVMInterOp LANGUAGES CXX)

# Imposta il percorso di LLVM
set(LLVM_DIR "${DEPS_DIR}/share/llvm")

# Imposta CMAKE_PREFIX_PATH se necessario
set(CMAKE_PREFIX_PATH "${DEPS_DIR}/share")

# Trova LLVM
find_package(LLVM REQUIRED CONFIG)

# Verifica che LLVM sia stato trovato
if(NOT LLVM_FOUND)
    message(FATAL_ERROR "LLVM non trovato!")
endif()

# Stampa il percorso di LLVM
message(STATUS "LLVM_DIR = ${LLVM_DIR}")
message(STATUS "LLVM_CMAKE_DIR = ${LLVM_CMAKE_DIR}")

# Aggiungi LLVM_CMAKE_DIR al CMAKE_MODULE_PATH
list(APPEND CMAKE_MODULE_PATH "${LLVM_CMAKE_DIR}")

# https://github.com/JonathanSalwan/Triton/issues/1082#issuecomment-1030826696
if(LLVM_LINK_LLVM_DYLIB)
  set(llvm_libs LLVM)
else()
   llvm_map_components_to_libnames(llvm_libs
    support
    core
    irreader
    bitreader
    bitwriter
    passes
    asmprinter
    aarch64info
    aarch64desc
    aarch64codegen
    aarch64asmparser
    armcodegen
    armasmparser
    HipStdPar
    CodeGenTypes
    interpreter
    mcjit
    FrontendOffloading
    nvptxdesc
    x86info
    x86codegen
    x86asmparser
    sparccodegen
    sparcasmparser
    webassemblydesc)
endif()
message(STATUS "LLVM Libraries: ${llvm_libs}")

message(STATUS "Found LLVM ${LLVM_PACKAGE_VERSION}")
message(STATUS "Using LLVMConfig.cmake in: ${LLVM_DIR}")

include_directories(SYSTEM ${LLVM_INCLUDE_DIRS})

# Impostazioni aggiuntive
include_directories(
    "${SRC_PATH}"
    "${DEPS_DIR}/include"
    "${KLEE_DIR}/install/include"
    "${SOUPER_DIR}/Install/include"
    "${REMILL_DIR}/install/include"
    ${LLVM_INCLUDE_DIRS}
)

link_directories(
    "${DEPS_DIR}/lib"
    "${REMILL_DIR}/install/lib"
    "${KLEE_DIR}/install/lib"
    "${SOUPER_DIR}/Install/lib"
    "${ALIVE2_LIBS_DIR}"
)

# Trova tutti i file .cpp, .h e .hpp nelle cartelle e sottocartelle di src
file(GLOB_RECURSE SOURCES
    "${SRC_PATH}/*.cpp"
    "${SRC_PATH}/*.h"
    "${SRC_PATH}/*.hpp"
)

# Verifica che la lista SOURCES non sia vuota
if(NOT SOURCES)
    message(FATAL_ERROR "Nessun file sorgente trovato in ${SRC_PATH}")
endif()

# Organizza i file sorgente in gruppi basati sulla struttura delle directory
source_group(TREE "${SRC_PATH}" FILES ${SOURCES})

# Verifica che la lista SOURCES non sia vuota
if(NOT SOURCES)
    message(FATAL_ERROR "Nessun file sorgente trovato in ${SRC_PATH}")
endif()

# Aggiungi la libreria condivisa (DLL)
add_library(LLVMInterOp SHARED ${SOURCES})

# Imposta lo standard C++17 per questo target
target_compile_features(LLVMInterOp PUBLIC cxx_std_17)

# Linka le librerie
target_link_libraries(LLVMInterOp PRIVATE ${llvm_libs})
target_link_libraries(LLVMInterOp PRIVATE
psapi.lib
shell32.lib
ole32.lib
#uuid.lib
advapi32.lib
delayimp.lib
kernel32.lib
user32.lib
gdi32.lib
winspool.lib
oleaut32.lib
comdlg32.lib
Ws2_32.lib
msvcrt.lib
)

target_link_libraries(LLVMInterOp PRIVATE
${DEPS_DIR}/lib/libz3.lib
${DEPS_DIR}/lib/xed.lib
#E:\cxx-common\Deps\installed\x64-windows-static-md-rel\lib\gflags_static.lib
#E:\cxx-common\Deps\installed\x64-windows-static-md-rel\lib\gflags_nothreads.lib
${DEPS_DIR}/lib/glog.lib
${REMILL_DIR}/install/lib/decomp.lib
${REMILL_DIR}/install/lib/remill_arch.lib
${REMILL_DIR}/install/lib/remill_arch_aarch64.lib
${REMILL_DIR}/install/lib/remill_arch_sleigh.lib
${REMILL_DIR}/install/lib/remill_arch_sparc32.lib
${REMILL_DIR}/install/lib/remill_arch_sparc64.lib
${REMILL_DIR}/install/lib/remill_arch_x86.lib
${REMILL_DIR}/install/lib/remill_bc.lib
${REMILL_DIR}/install/lib/remill_os.lib
${REMILL_DIR}/install/lib/remill_version.lib
${REMILL_DIR}/install/lib/sla.lib
${REMILL_DIR}/install/lib/slaSupport.lib
${KLEE_DIR}/install/lib/kleeBasic.lib
${KLEE_DIR}/install/lib/kleeCore.lib
${KLEE_DIR}/install/lib/kleeModule.lib
${KLEE_DIR}/install/lib/kleeSupport.lib
${KLEE_DIR}/install/lib/kleaverSolver.lib
${SOUPER_DIR}/install/lib/kleeExpr.lib
${SOUPER_DIR}/install/lib/souperCodegen.lib
${SOUPER_DIR}/install/lib/souperExtractor.lib
${SOUPER_DIR}/install/lib/souperInfer.lib
${SOUPER_DIR}/install/lib/souperInst.lib
${SOUPER_DIR}/install/lib/souperParser.lib
${SOUPER_DIR}/install/lib/souperSMTLIB2.lib
${ALIVE2_LIBS_DIR}/alive2.lib
${ALIVE2_LIBS_DIR}/ir.lib
${ALIVE2_LIBS_DIR}/smt.lib
${ALIVE2_LIBS_DIR}/tools.lib
${ALIVE2_LIBS_DIR}/util.lib
)

if(WIN32)
    target_link_libraries(LLVMInterOp INTERFACE Ws2_32.lib)
endif()

# Imposta le proprietà per la DLL su Windows
if(WIN32)
    set_target_properties(LLVMInterOp PROPERTIES WINDOWS_EXPORT_ALL_SYMBOLS ON)
endif()

if(WIN32)
  # warnings and compiler settings
  target_compile_options(LLVMInterOp PRIVATE
    "$<$<CONFIG:Debug>:/MDd>$<$<CONFIG:Release>:/MD>"
    /nologo /W3 /EHsc /wd4141 /wd4146 /wd4180 /wd4244
    /wd4258 /wd4267 /wd4291 /wd4345 /wd4351 /wd4355 /wd4456
    /wd4457 /wd4458 /wd4459 /wd4503 /wd4624 /wd4722 /wd4800
    /wd4100 /wd4127 /wd4512 /wd4505 /wd4610 /wd4510 /wd4702
    /wd4245 /wd4706 /wd4310 /wd4701 /wd4703 /wd4389 /wd4611
    /wd4805 /wd4204 /wd4577 /wd4091 /wd4592 /wd4324
  )

  target_compile_definitions(LLVMInterOp PRIVATE
    _CRT_SECURE_NO_DEPRECATE
    _CRT_SECURE_NO_WARNINGS
    _CRT_NONSTDC_NO_DEPRECATE
    _CRT_NONSTDC_NO_WARNINGS
    _SCL_SECURE_NO_DEPRECATE
    _SCL_SECURE_NO_WARNINGS
    GOOGLE_PROTOBUF_NO_RTTI
  )

else()
  # warnings and compiler settings
  target_compile_options(LLVMInterOp PRIVATE
    -Wall -Wextra -Wno-unused-parameter -Wno-c++98-compat
    -Wno-unreachable-code-return -Wno-nested-anon-types
    -Wno-extended-offsetof
    -Wno-variadic-macros -Wno-return-type-c-linkage
    -Wno-c99-extensions -Wno-ignored-attributes -Wno-unused-local-typedef
    -Wno-unknown-pragmas -Wno-unknown-warning-option -fPIC
    -fno-omit-frame-pointer -fvisibility-inlines-hidden
    -fno-asynchronous-unwind-tables
  )
endif()

# Aggiungi definizioni del compilatore
target_compile_definitions(LLVMInterOp PRIVATE NOMINMAX)
