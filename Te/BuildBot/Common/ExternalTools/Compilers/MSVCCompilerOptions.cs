/// The MIT License (MIT) Copyright (c) 2016 Jesse Nicholson 
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a 
/// copy of this software and associated documentation files (the 
/// "Software"), to deal in the Software without restriction, including 
/// without limitation the rights to use, copy, modify, merge, publish, 
/// distribute, sublicense, and/or sell copies of the Software, and to 
/// permit persons to whom the Software is furnished to do so, subject to 
/// the following conditions: 
/// 
/// The above copyright notice and this permission notice shall be included 
/// in all copies or substantial portions of the Software. 
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
/// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
/// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
/// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY 
/// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
/// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
/// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

/* 

This file is a huge XXX TODO.
All compiler options specified at:
https://msdn.microsoft.com/en-us/library/fwkeyyhe.aspx

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BuildBotCore
{
    namespace Common
    {
        namespace ExternalTools
        {
            namespace Compilers
            {

                /// <summary>
                /// /// The MSVCCompilerOptions class is used to configure arguments
                /// passed to the MSVC compiler on execution.
                /// </summary>
                /// <remarks>
                /// Options taken from:
                /// https://msdn.microsoft.com/en-us/library/fwkeyyhe.aspx
                /// </remarks>
                public class MSVCCompilerOptions
                {
                    public enum ClrType
                    {
                        None,
                        Clr,
                        ClrPure,
                        ClrSafe,
                        ClrNoAssembly,
                        ClrInitialAppDomain,
                        ClrNoStdLib
                    }

                    /// <summary>
                    /// Specifies various types of exception handling available.
                    /// </summary>
                    [Flags]
                    public enum ExceptionHandlingParameters
                    {
                        /// <summary>
                        /// Catches both asynchronous and synchronous (C++)
                        /// exceptions.
                        /// </summary>
                        a = 1,

                        /// <summary>
                        /// Catches C++ exceptions only an assumes extern "C"
                        /// functions can throw.
                        /// </summary>
                        s = 2,

                        /// <summary>
                        /// If used with s (/EHsc), catches C++ exceptions only
                        /// and assumes that extern "C" declared functions will
                        /// never throw a C++ exception.
                        /// </summary>
                        c = 4,

                        /// <summary>
                        /// Tells the compiler to always generate runtime
                        /// termination checks for all noexcept functions. The
                        /// default value will cause the compiler to possibly
                        /// optimize away runtime checks for noexcept functions
                        /// when it determines it safe to do see.
                        /// </summary>
                        r = 8
                    }

                    /// <summary>
                    /// Targets for which the compiler supports specific optimizations.
                    /// </summary>
                    public enum OptimizeTargets
                    {
                        /// <summary>
                        /// For x86 and x64, produces code that is most optimal
                        /// and portable across Intel and AMD architectures.
                        /// This is the default value.
                        /// </summary>
                        blend,

                        /// <summary>
                        /// For x86 and x64, produces code optimized for Intel
                        /// ATOM and Centrino processors. Code compiled with
                        /// this flag might also produce SSSE3, SSE3, SSE2, and
                        /// SSE instructions.
                        /// </summary>
                        ATOM,

                        /// <summary>
                        /// For x64 only, produces code that is most optimal for
                        /// AMD Opteron and Ahtlon processors that support
                        /// 64-bit extensions. While generated instructions will
                        /// function on all 64-bit platforms, this code might
                        /// function with poor performance on Intel processors
                        /// that support Intel64.
                        /// </summary>
                        AMD64,

                        /// <summary>
                        /// For x64 only, produces code that is most optimal for
                        /// Intel processors that support Intel64. While
                        /// generated instructions will function on all 64-bit
                        /// platforms, this code might function with poor
                        /// performance on AMD Opteron and Athlon processors
                        /// that support 64-bit extensions.
                        /// </summary>
                        INTEL64
                    }

                    /// <summary>
                    /// Arguments that control the generation of source code,
                    /// machine code and the extension of the listing file.
                    /// </summary>
                    [Flags]
                    public enum ListingFileTypes
                    {
                        /// <summary>
                        /// Do not generate a listing file. Default.
                        ///</summary>
                        None = 1,

                        /// <summary>
                        /// Assembly only, .asm extension. Default when only /FA is specified.
                        /// </summary>
                        /// <remarks>
                        /// Represents the /FA flag.
                        /// </remarks>
                        Assembly = 2,

                        /// <summary>
                        /// Machine and assembly, .cod extension.
                        /// </summary>
                        /// <remarks>
                        /// Represents the /FAc flag.
                        /// </remarks>
                        AssemblyAndMachine = 4,

                        /// <summary>
                        /// Source and assembly, .asm extension. However, if the
                        /// c flag is also supplied, represented here in code by
                        /// the AssemblyAndMachine flag, then the file extension
                        /// will be .cod.
                        /// </summary>
                        /// <remarks>
                        /// Represents the /FAs flag.
                        /// </remarks>
                        SourceAndAssembly = 8,

                        /// <summary>
                        /// Causes output to be created in UTF-8 format with a
                        /// byte order marker. Default is ANSI. If specified
                        /// with /FAu, the u flag herein represented by the
                        /// SourceAndAssembly flag, then the generated output
                        /// may not display correctly.
                        /// </summary>
                        /// <remarks>
                        /// Represents the /FAu flag.
                        /// </remarks>
                        Utf8 = 16
                    }

                    /// <summary>
                    /// Holds the actual string compiler switches for each
                    /// publicly available property.
                    /// </summary>
                    private static readonly Dictionary<string, string> CompilerFlagsStringMap;

                    static MSVCCompilerOptions()
                    {
                        // Init & populate mapping.
                        CompilerFlagsStringMap = new Dictionary<string, string>();

                        CompilerFlagsStringMap.Add(nameof(ResponseFile), "@");
                        CompilerFlagsStringMap.Add(nameof(MetadataDirectories), "/AI");
                        CompilerFlagsStringMap.Add(nameof(Analysis), "/analyze");
                        CompilerFlagsStringMap.Add(nameof(BigObj), "/bigobj");
                        CompilerFlagsStringMap.Add(nameof(PreserveCommentsInPreprocessing), "/C");
                        CompilerFlagsStringMap.Add(nameof(NoLinking), "/c");
                        CompilerFlagsStringMap.Add(nameof(CgThreads), "/cgthreads");
                        CompilerFlagsStringMap.Add(nameof(Clr), "/clr");
                        CompilerFlagsStringMap.Add(nameof(Defines), "/D");
                        CompilerFlagsStringMap.Add(nameof(Doc), "/doc");
                        CompilerFlagsStringMap.Add(nameof(PreprocessStdOutputWithLineDirectives), "/E");
                        CompilerFlagsStringMap.Add(nameof(PreprocessStdOutputWithoutLineDirectives), "/EP");
                        CompilerFlagsStringMap.Add(nameof(ExceptionHandling), "/EH");
                        CompilerFlagsStringMap.Add(nameof(StackSize), "/F");
                        CompilerFlagsStringMap.Add(nameof(Favor), "/favor");
                        CompilerFlagsStringMap.Add(nameof(ListingFile), "/FA");
                        CompilerFlagsStringMap.Add(nameof(ListingFileNaming), "/Fa");
                        CompilerFlagsStringMap.Add(nameof(FullSourcePaths), "/FC");
                        CompilerFlagsStringMap.Add(nameof(ProgramDatabaseName), "/Fd");
                        CompilerFlagsStringMap.Add(nameof(OutputAssemblyName), "/Fe");
                    }

                    public MSVCCompilerOptions()
                    {        
                        // Init default values.
                        ResponseFile = string.Empty;

                        MetadataDirectories = new List<string>();

                        // XXX TODO - Expand this argument into typed structure.
                        Analysis = new List<string>();

                        BigObj = false;

                        PreserveCommentsInPreprocessing = false;
                        
                        NoLinking = false;

                        CgThreads = 1;

                        Clr = ClrType.None;

                        Defines = new List<string>();

                        Doc = false;

                        PreprocessStdOutputWithLineDirectives = false;
                        PreprocessStdOutputWithoutLineDirectives = false;

                        // Default to async. XXX TODO - Double check this. No clear docs.
                        ExceptionHandling = ExceptionHandlingParameters.a;

                        // Set default as 1MB.
                        StackSize = 1048576;

                        Favor = OptimizeTargets.blend;

                        ListingFile = ListingFileTypes.None;

                        ListingFileNaming = string.Empty;

                        FullSourcePaths = false;

                        ProgramDatabaseName = string.Empty;

                        OutputAssemblyName = string.Empty;
                    }

                    /// <summary>
                    /// Specifies a response file. A response file is a text
                    /// file containing compiler commands.
                    /// </summary>  
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/3te4xt0y.aspx
                    ///
                    /// Represents the @ switch, which points to a text file
                    /// containing compiler commands.
                    /// </remarks>                  
                    public string ResponseFile
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Specifies one or more directory that the compiler will
                    /// search to resolve file references passed to the #using
                    /// directive.
                    /// </summary>  
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/x1x72k9t.aspx
                    ///
                    /// Represents the /AI switch, of which each instance points
                    /// to a directory to search for files referenced by the
                    /// using directive.
                    /// </remarks>                  
                    public List<string> MetadataDirectories
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Enables code analysis and control options.
                    /// </summary>  
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/ms173498.aspx
                    ///
                    /// Represents the /analyze switch.
                    /// </remarks>                  
                    public List<string> Analysis
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Increases the number of sections that an object file can contain.
                    /// </summary>  
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/ms173499.aspx
                    ///
                    /// Represents the /bigobj switch.
                    /// </remarks>  
                    public bool BigObj
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Preserves comments during preprocessing.
                    /// </summary>  
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/32165865.aspx
                    ///
                    /// Represents the /C switch. Requires the /E, /P, or /EP option. 
                    /// </remarks>  
                    public bool PreserveCommentsInPreprocessing
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Prevents the automatic call to LINK.
                    /// </summary>  
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/8we9bhf4.aspx
                    ///
                    /// Represents the /c switch.
                    /// </remarks> 
                    public bool NoLinking
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// The number of cl.exe threads to use for optimization
                    /// and code generation.
                    /// </summary>  
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/dn631956.aspx
                    ///
                    /// Represents the /cgthreads switch.
                    /// </remarks> 
                    public byte CgThreads
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Defines CLR support during compilation.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/k8d11d4s.aspx
                    ///
                    /// Represents the /cgthreads switch.
                    /// </remarks> 
                    public ClrType Clr
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Defines a preprocessing symbol for a source file.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/hhzbb5c8.aspx
                    ///
                    /// Represents the /D switch.
                    /// </remarks>
                    public List<string> Defines
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Gets whether the compiler will process documentation
                    /// comments in source code files and to create an .xdc file
                    /// for each source code file that has documentation
                    /// comments.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/ms173501.aspx
                    ///
                    /// Represents the /doc switch.
                    /// </remarks>
                    public bool Doc
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// If the Doc option is enabled, specifies the file that
                    /// comments are to be written to. Only valid when one .cpp
                    /// file is passed in the compilation.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/ms173501.aspx
                    ///
                    /// Represents an optional argument to the /doc switch.
                    /// </remarks>
                    public string DocFileName
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// If enabled, instructs the compiler to preprocess C and
                    /// C++ source files and copy the preprocessed files to the
                    /// standard output device. Includes #line directives.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/3xkfswhy.aspx
                    ///
                    /// Represents the /E switch.
                    /// </remarks>
                    public bool PreprocessStdOutputWithLineDirectives
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// If enabled, instructs the compiler to preprocess C and
                    /// C++ source files and copy the preprocessed files to the
                    /// standard output device. Excludes #line directives.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/becb7sys.aspx
                    ///
                    /// Represents the /EP switch.
                    /// </remarks>
                    public bool PreprocessStdOutputWithoutLineDirectives
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Specifies the kind of exception handling used by the
                    /// compiler.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/1deeycx5.aspx
                    ///
                    /// Represents the /EH switch.
                    /// </remarks>
                    public ExceptionHandlingParameters ExceptionHandling
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Sets the program stack size in bytes. Defaults to 1MB.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/tdkhxaks.aspx
                    ///
                    /// Represents the /F switch.
                    /// </remarks>
                    public uint StackSize
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Produces the code that is optimized for a specific
                    /// architecture or micro-architecture.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/ms173505.aspx
                    ///
                    /// Represents the /favor switch.
                    /// </remarks>
                    public OptimizeTargets Favor
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Defines whether to create a listing file containing
                    /// assembly code.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/367y26c6.aspx
                    ///
                    /// Represents the /FA switch and variants.
                    /// </remarks>
                    public ListingFileTypes ListingFile
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Determines the naming convention to be used when
                    /// generating listing files. By default, one
                    /// source_file.asm or source_file.cod are created per
                    /// processed source code file.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/367y26c6.aspx
                    ///
                    /// Represents the /Fa switch and variants. Specify nothing
                    /// for this argument for default behavior. For anything
                    /// else, follow the string formatting noted at the linked
                    /// documentation page.
                    /// </remarks>
                    public string ListingFileNaming
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// Whether or not to diplay full paths to source code files
                    /// passed to the compiler in diagnostics.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/027c4t2s.aspx
                    ///
                    /// Represents the /FC switch.
                    /// </remarks>
                    public bool FullSourcePaths
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// A custom name and directory for the program database
                    /// file.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/9wst99a9.aspx
                    ///
                    /// Represents the /Fd switch.
                    /// </remarks>
                    public string ProgramDatabaseName
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// A custom name and directory for the generated .exe or
                    /// .dll.
                    /// </summary>
                    /// <remarks>
                    /// Defined here:
                    /// https://msdn.microsoft.com/en-us/library/kc584260.aspx
                    ///
                    /// Represents the /Fe switch.
                    /// </remarks>
                    public string OutputAssemblyName
                    {
                        get;
                        private set;
                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="directoryPath"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithMetadataDirectory(string directoryPath)
                    {
                        Debug.Assert(!string.IsNullOrEmpty(directoryPath) && !string.IsNullOrWhiteSpace(directoryPath), "Supplied directory cannot be null, empty or whitespace.");

                        if (string.IsNullOrEmpty(directoryPath) || string.IsNullOrWhiteSpace(directoryPath))
                        {
                            throw new ArgumentException("Supplied directory cannot be null, empty or whitespace.", nameof(directoryPath));
                        }

                        MetadataDirectories.Add(directoryPath);
                        return this;
                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="filePath"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithResponseFile(string filePath)
                    {
                        ResponseFile = filePath;
                        return this;
                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="numThreads"></param>
                    /// <returns></returns>
                    /// <exception cref="ArgumentException">
                    /// Argument value must be within the range of 1-8. In the
                    /// event that it is not, the setter for this property will
                    /// throw.
                    /// </exception>
                    public MSVCCompilerOptions WithCgThreads(byte numThreads)
                    {
                        Debug.Assert(numThreads > 0 && numThreads < 9, "Value must be within the range 1-8.");

                        if(numThreads < 1 || numThreads > 8)
                        {
                            throw new ArgumentException("Value must be within the range 1-8.", nameof(numThreads));
                        }
                        
                        CgThreads = numThreads;
                        return this;
                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="key"></param>
                    /// <param name="value"></param>
                    /// <returns></returns>
                    /// <exception cref="ArgumentException"> TODO Note about percent sign escaping.</exception>
                    public MSVCCompilerOptions WithDefine(string key, string value)
                    {

                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="enabled"></param>
                    /// <param name="docFileName"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithDocGeneration(bool enabled, string docFileName = null)
                    {

                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="withLineDirectives"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithPreprocessorStdOutput(bool withLineDirectives)
                    {

                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="exceptionParams"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithExceptionHandling(ExceptionHandlingParameters exceptionParams)
                    {

                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="stackSize"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithStackSize(uint stackSize)
                    {

                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="option"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithFavor(OptimizeTargets option)
                    {

                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="option"></param>
                    /// <returns></returns>
                    /// <exception cref="ArgumentException">
                    /// XXX TODO - Make note about how cannot be None + others.
                    /// </exception>
                    public MSVCCompilerOptions WithListingFile(ListingFileTypes option)
                    {
                        
                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="enabled"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithFullSourcePaths(bool enabled)
                    {

                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="pathAndFileName"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithCustomProgramDatabase(string pathAndFileName)
                    {

                    }

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <param name="pathAndFileName"></param>
                    /// <returns></returns>
                    public MSVCCompilerOptions WithCustomOutputAssembly(string pathAndFileName)
                    {
                        
                    }
                }
            }
        }
    }
}

*/