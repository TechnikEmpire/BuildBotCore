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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BuildBot.Extensions;

namespace BuildBotCore
{
    namespace Common
    {
        namespace ExternalTools
        {
            namespace Compilers
            {

                /// <summary>
                /// The MSVCCompilerTask class represents a generic interface to
                /// the MSVC compiler. This task correctly loads and configures
                /// internally used environmental variables to ensure correct
                /// compilation for all supported Visual Studio versions and
                /// compilation target architectures.
                /// </summary>
                public class MSVCCompilerTask : AbstractCompilerTask
                {

                    /// <summary>
                    /// Some default release flags common to all project types.
                    /// </summary>
                    public static readonly List<string> DefaultReleaseCompilerFlags = new List<string>()
                    {
                        "/Ox",
                        "/Oi",
                        "/Ot",
                        "/GF",
                        "/fp:fast"
                    };

                    /// <summary>
                    /// Some common debug flags common to all project types.
                    /// </summary>
                    public static readonly List<string> DefaultDebugCompilerFlags = new List<string>()
                    {
                        "/Od"
                    };

                    /// <summary>
                    /// Represents various Visual Studio versions where we'll look to invoke
                    /// cl.exe.
                    /// </summary>
                    public enum ToolVersions
                    {
                        /// <summary>
                        /// Visual Studio 2012.
                        /// </summary>      
                        v11 = 0,

                        /// <summary>
                        /// Visual Studio 2013.
                        /// </summary>      
                        v12 = 1,

                        /// <summary>
                        /// Visual Studio 2015.
                        /// </summary>      
                        v14 = 2
                    }

                    /// <summary>
                    /// Gets or sets the minimum required Visual Studio version for this
                    /// compilation task.
                    /// </summary>
                    public ToolVersions MinimumRequiredToolVersion
                    {
                        get;
                        set;
                    }

                    public override Architecture SupportedArchitectures
                    {
                        get
                        {
                            return Architecture.x64 | Architecture.x86;
                        }
                    }

                    public override Guid GUID
                    {
                        get
                        {
                            return Guid.Parse("c3488e6e-6b85-4b1e-bca0-4f7f646cd638");
                        }
                    }

                    public override string Help
                    {
                        get
                        {
                            return "The MSVCCompilerTask is meant to be used internally by another build task.";
                        }
                    }

                    public override bool IsOsPlatformSupported
                    {
                        get
                        {
                            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                            return isWindows;
                        }
                    }

                    public override List<Guid> TaskDependencies
                    {
                        get
                        {
                            // Depends on nothing.
                            return new List<Guid>();
                        }
                    }

                    public override string TaskFriendlyName
                    {
                        get
                        {
                            return "MSVC Compiler Task";
                        }
                    }

                    /// <summary>
                    /// Constructs a new MSVCCompilerTask object.
                    /// </summary>
                    /// <param name="scriptAbsolutePath">
                    /// The absolute path to this build task script. This is supplied by the
                    /// master BuildBot application to every build script discovered and
                    /// compiled on instantiation to provide a file system context to the
                    /// build script.
                    /// </param>
                    public MSVCCompilerTask(string scriptAbsolutePath) : base(scriptAbsolutePath)
                    {

                    }

                    /// <summary>
                    /// Runs the clean action for the compiler. In this case we
                    /// simply want to delete our intermediaries directory.
                    /// </summary>
                    /// <returns>
                    /// True if the intermediaries directory deletion and
                    /// recreation was a success, false otherwise.
                    /// </returns>
                    public override bool Clean()
                    {
                        // Clean exceptions first.
                        Errors.Clear();

                        try
                        {
                            Directory.Delete(IntermediaryDirectory);
                            Directory.CreateDirectory(IntermediaryDirectory);
                            return true;
                        }
                        catch (Exception e)
                        {
                            var cleanException = new Exception("Failed to run clean action. See inner exception for details.", e);
                            Errors.Add(cleanException);
                        }

                        return false;
                    }

                    /// <summary>
                    /// Runs the MSVC compiliation task for all given
                    /// configurations and architecture targets.
                    /// </summary>
                    /// <param name="config">
                    /// The configurations to compile against.
                    /// </param>
                    /// <param name="arch">
                    /// The architectures to use in assembly generation.
                    /// </param>
                    /// <returns>
                    /// True if the build task succeeded, false otherwise.
                    /// </returns>
                    public override bool Run(BuildConfiguration config, Architecture arch)
                    {
                        // Clean exceptions first.
                        Errors.Clear();

                        // First thing is to ensure that an acceptable version of MSVC is installed.
                        var installedVcVersions = GetInstalledToolVersions();

                        if (!installedVcVersions.ContainsKey(MinimumRequiredToolVersion))
                        {
                            Errors.Add(new Exception(string.Format("An installation of MSVC meeting the minimum required version of {0} could not be found.", MinimumRequiredToolVersion.ToString())));
                            return false;
                        }

                        // Ensure that we have an output directory.
                        Debug.Assert(!string.IsNullOrEmpty(OutputDirectory) && !string.IsNullOrWhiteSpace(OutputDirectory), "No output directory defined.");
                        if (string.IsNullOrEmpty(OutputDirectory) || string.IsNullOrWhiteSpace(OutputDirectory))
                        {
                            Errors.Add(new Exception("No output directory defined."));
                            return false;
                        }

                        // Ensure that we have an output file name.
                        Debug.Assert(!string.IsNullOrEmpty(OutputFileName) && !string.IsNullOrWhiteSpace(OutputFileName), "No output file name defined.");
                        if (string.IsNullOrEmpty(OutputFileName) || string.IsNullOrWhiteSpace(OutputFileName))
                        {
                            Errors.Add(new Exception("No output fine name defined."));
                            return false;
                        }

                        // Ensure we have sources to compile.
                        Debug.Assert(Sources.Count > 0, "No sources defined.");
                        if (Sources.Count <= 0)
                        {
                            Errors.Add(new Exception("No sources defined."));
                            return false;
                        }

                        // Make clones of all the various falgs. The reason for this
                        // is because the user might have specified non-target, non-plat
                        // specific flags, which we want to apply after we clear/reset
                        // them ever time we compile for a config.
                        var originalGlobalCompilerFlags = new List<string>(GlobalCompilerFlags);
                        var originalGlobalLinkerFlags = new List<string>(GlobalLinkerFlags);

                        var originalReleaseCompilerFlags = new List<string>(ReleaseCompilerFlags);
                        var originalDebugCompilerFlags = new List<string>(DebugCompilerFlags);

                        var originalReleaseLinkerFlags = new List<string>(ReleaseLinkerFlags);
                        var originalDebugLinkerFlags = new List<string>(DebugLinkerFlags);

                        // Keep track of number of configs/archs we build for. Use
                        // these counts to determine success.
                        int totalCompilationAttempts = 0;
                        int totalCompilationSuccess = 0;

                        // Iterate over set configuration flags. Then iterate over set
                        // arch flags, build out ENV for each configuration and run the
                        // build with that environment.
                        foreach (BuildConfiguration cfg in Enum.GetValues(typeof(BuildConfiguration)))
                        {
                            if (config.HasFlag(cfg))
                            {
                                foreach (Architecture a in Enum.GetValues(typeof(Architecture)))
                                {
                                    if (arch.HasFlag(a))
                                    {
                                        // Begin a compilation attempt for config/arch.
                                        ++totalCompilationAttempts;

                                        Console.WriteLine(string.Format("Running MSVC Compilation for config: {0} and arch: {1}.", cfg.ToString(), a.ToString()));

                                        // Reset flags to defaults.
                                        GlobalCompilerFlags = new List<string>(originalGlobalCompilerFlags);
                                        GlobalLinkerFlags = new List<string>(originalGlobalLinkerFlags);
                                        ReleaseCompilerFlags = new List<string>(originalReleaseCompilerFlags);
                                        DebugCompilerFlags = new List<string>(originalDebugCompilerFlags);
                                        ReleaseLinkerFlags = new List<string>(originalReleaseLinkerFlags);
                                        DebugLinkerFlags = new List<string>(originalDebugLinkerFlags);

                                        // Add debug/release flags depending on current CFG.
                                        switch (cfg)
                                        {
                                            case BuildConfiguration.Debug:
                                                {
                                                    GlobalCompilerFlags = GlobalCompilerFlags.Concat(DebugCompilerFlags).ToList();
                                                }
                                                break;

                                            case BuildConfiguration.Release:
                                                {
                                                    GlobalCompilerFlags = GlobalCompilerFlags.Concat(ReleaseCompilerFlags).ToList();
                                                }
                                                break;
                                        }

                                        // Build intermediary directory
                                        string fullIntermediaryDir = string.Empty;

                                        if (!string.IsNullOrEmpty(IntermediaryDirectory) && !string.IsNullOrWhiteSpace(IntermediaryDirectory))
                                        {
                                            fullIntermediaryDir = IntermediaryDirectory.ConvertToHostOsPath();
                                        }
                                        else
                                        {
                                            // If not specified, use working directory.
                                            fullIntermediaryDir = WorkingDirectory;
                                        }

                                        // Build out so that it's Debug/Release x86/x64
                                        fullIntermediaryDir = fullIntermediaryDir + Path.DirectorySeparatorChar + string.Format("{0} {1}", cfg.ToString(), a.ToString());

                                        // Now we need the ENV vars for our tool version.
                                        var envVars = GetEnvironmentVariables(MinimumRequiredToolVersion, installedVcVersions[MinimumRequiredToolVersion], a);

                                        // Force creation of intermediaries directory.
                                        Directory.CreateDirectory(fullIntermediaryDir);

                                        // Force creation of output directory for arch/config.
                                        string configArchOutDir = string.Format("{0}{1} {2}",
                                            OutputDirectory.ConvertToHostOsPath() + Path.DirectorySeparatorChar + "msvc" + Path.DirectorySeparatorChar,
                                            cfg.ToString(), a.ToString());

                                        // Set the intermediary output directory. We use a trailing slash
                                        // here because if we don't, the params won't parse properly when
                                        // passed to CL.exe.
                                        GlobalCompilerFlags.Add(string.Format("/Fo\"{0}/\"", fullIntermediaryDir));

                                        switch (a)
                                        {
                                            case Architecture.x86:
                                                {
                                                    GlobalLinkerFlags.Add("/MACHINE:x86");
                                                }
                                                break;

                                            case Architecture.x64:
                                                {
                                                    GlobalLinkerFlags.Add("/MACHINE:x64");
                                                }
                                                break;
                                        }

                                        // Whether we're merging the link command.
                                        bool isStaticLibrary = false;

                                        // Setup final output path. Create it first.
                                        Directory.CreateDirectory(configArchOutDir);
                                        string finalOutputPath = configArchOutDir + Path.DirectorySeparatorChar + OutputFileName;


                                        switch (OutputAssemblyType)
                                        {
                                            case AssemblyType.Unspecified:
                                                {
                                                    Errors.Add(new Exception("No output assembly type specified."));
                                                    return false;
                                                }

                                            case AssemblyType.SharedLibrary:
                                                {
                                                    // Define User/Win DLL.
                                                    GlobalCompilerFlags.Add("/D_USRDLL");
                                                    GlobalCompilerFlags.Add("/D_WINDLL");

                                                    GlobalLinkerFlags.Add("/DLL");

                                                    isStaticLibrary = true;

                                                    // Add appropriate file extension.
                                                    finalOutputPath += ".dll";
                                                }
                                                break;

                                            case AssemblyType.StaticLibrary:
                                                {
                                                    // /c flag means don't link
                                                    if (!GlobalCompilerFlags.Contains("/c"))
                                                    {
                                                        GlobalCompilerFlags.Add("/c");
                                                    }

                                                    // Add appropriate file extension.
                                                    finalOutputPath += ".lib";
                                                }
                                                break;

                                            case AssemblyType.Executable:
                                                {
                                                    isStaticLibrary = true;

                                                    // Add appropriate file extension.
                                                    finalOutputPath += ".exe";
                                                }
                                                break;
                                        }

                                        // Specify full output path. Spaces escaped later.
                                        GlobalLinkerFlags.Add(string.Format("/OUT:\"{0}\"", finalOutputPath));

                                        // Add debug/release flags to linker depending on current CFG.
                                        switch (cfg)
                                        {
                                            case BuildConfiguration.Debug:
                                                {
                                                    GlobalLinkerFlags = GlobalLinkerFlags.Concat(DebugLinkerFlags).ToList();
                                                }
                                                break;

                                            case BuildConfiguration.Release:
                                                {
                                                    GlobalLinkerFlags = GlobalLinkerFlags.Concat(ReleaseLinkerFlags).ToList();
                                                }
                                                break;
                                        }

                                        // Clone compile flags.
                                        var finalArgs = new List<string>(GlobalCompilerFlags);

                                        // Append includes.
                                        foreach (var inclPath in IncludePaths)
                                        {
                                            // Escape spaces in path.
                                            finalArgs.Add("/I");
                                            finalArgs.Add(inclPath.Replace(" ", @"\\ "));
                                        }

                                        // Append sources.
                                        finalArgs = finalArgs.Concat(Sources).ToList();

                                        if (isStaticLibrary)
                                        {
                                            // Add linker call if applicable.                           
                                            finalArgs.Add("/link");
                                            finalArgs = finalArgs.Concat(GlobalLinkerFlags).ToList();
                                        }
                                        else
                                        {
                                            // Add debug/release DLL flags depending on current CFG.
                                            // Shared runtime, as long as this isn't a static lib.
                                            switch (cfg)
                                            {
                                                case BuildConfiguration.Debug:
                                                    {
                                                        finalArgs.Add("/MDd");
                                                    }
                                                    break;

                                                case BuildConfiguration.Release:
                                                    {
                                                        finalArgs.Add("/MD");
                                                    }
                                                    break;
                                            }
                                        }

                                        // Run compiler.
                                        string clExe = "cl.exe";
                                        var paths = envVars["PATH"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                                        // Need to obtain a full path to cl.exe
                                        var fullClPath = paths.Select(x => Path.Combine(x, clExe))
                                            .Where(x => File.Exists(x))
                                            .FirstOrDefault();

                                        if (!string.IsNullOrEmpty(fullClPath) && !string.IsNullOrWhiteSpace(fullClPath))
                                        {
                                            clExe = fullClPath;
                                        }
                                        else
                                        {
                                            Errors.Add(new Exception("Could not find absolute path to cl.exe"));
                                            return false;
                                        }

                                        // Print final args.
                                        //Console.WriteLine(string.Join(" ", finalArgs));

                                        int compileReturnCode = RunProcess(WorkingDirectory, clExe, finalArgs, Timeout.Infinite, envVars);

                                        // If no auto linking, run LIB.
                                        if (!isStaticLibrary && compileReturnCode == 0)
                                        {
                                            // Basically means static library. So, we need to run
                                            // LIB tool.
                                            string libExe = "lib.exe";

                                            // Need to obtain a full path to lib.exe
                                            var fullLibPath = paths.Select(x => Path.Combine(x, libExe))
                                                .Where(x => File.Exists(x))
                                                .FirstOrDefault();

                                            if (!string.IsNullOrEmpty(fullLibPath) && !string.IsNullOrWhiteSpace(fullLibPath))
                                            {
                                                libExe = fullLibPath;
                                            }
                                            else
                                            {
                                                Errors.Add(new Exception("Could not find absolute path to lib.exe"));
                                                return false;
                                            }

                                            var finalLibArgs = new List<string>();

                                            foreach (var libDir in LibraryPaths)
                                            {
                                                // Add all lib dirs with escaped spaces.
                                                finalLibArgs.Add(string.Format("/LIBPATH {0}", libDir.Replace(" ", @"\\ ")));
                                            }

                                            // Add all of our generated objects                                            
                                            foreach (var objFile in Directory.GetFiles(fullIntermediaryDir))
                                            {
                                                finalLibArgs.Add(string.Format("\"{0}\"", objFile));
                                            }

                                            foreach (var linkerArg in GlobalLinkerFlags)
                                            {
                                                // Add all linker args with escaped spaces.
                                                finalLibArgs.Add(linkerArg);
                                            }

                                            // Print all final linker args.
                                            // Console.WriteLine(string.Join(" ", finalLibArgs));

                                            int libReturnCode = RunProcess(WorkingDirectory, libExe, finalLibArgs, Timeout.Infinite, envVars);

                                            if (libReturnCode != 0)
                                            {
                                                return false;
                                            }
                                        }

                                        // CL always returns 0 for success. Any other value is failure:
                                        // Source:
                                        // https://msdn.microsoft.com/en-us/library/ebh0y918.aspx
                                        if (compileReturnCode != 0)
                                        {
                                            return false;
                                        }
                                        else
                                        {
                                            ++totalCompilationSuccess;
                                        }

                                    } // End of if building for arch.
                                } // End of foreach architecture.
                            } // End of if building for config.
                        } // End of foreach config.

                        bool success = totalCompilationAttempts > 0 && totalCompilationSuccess == totalCompilationAttempts;

                        if (success)
                        {
                            // Copy includes if we've successfully built a library and includes is specified.                                        
                            switch (OutputAssemblyType)
                            {
                                case AssemblyType.SharedLibrary:
                                case AssemblyType.StaticLibrary:
                                    {
                                        // Only if auto copy is true.
                                        if (AutoCopyIncludes)
                                        {

                                            // Copy all .h, .hpp, .hxx etc. 
                                            HashSet<string> exclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                            exclusions.Add(".c");
                                            exclusions.Add(".cpp");
                                            exclusions.Add(".cxx");

                                            foreach (var includePath in IncludePaths)
                                            {
                                                CopyDirectory(includePath, OutputDirectory + Path.DirectorySeparatorChar + "include", true, true, null, exclusions);
                                            }
                                        }
                                    }
                                    break;

                                default:
                                    break;
                            }

                        }
                        // Always assume failure first.
                        return success;
                    }

                    /// <summary>
                    /// Gets a list of all discovered, installed version of
                    /// Visual Studio with Visual C/C++ support.
                    /// </summary>
                    /// <returns>
                    /// A dictionary of all discovered, installed version of Visual
                    /// Studio with Visual C/C++ support. The keys are the ToolVersions
                    /// enum, and the values are the path to the cl.exe bin directory
                    /// where the version install was discovered.
                    /// </returns>
                    private Dictionary<ToolVersions, string> GetInstalledToolVersions()
                    {

                        var installedVersions = new Dictionary<ToolVersions, string>();

                        foreach (ToolVersions toolVer in Enum.GetValues(typeof(ToolVersions)))
                        {

                            // Check to see if there's an environmental variable for each
                            // version of Visual Studio we list.
                            var vIntString = Regex.Replace(toolVer.ToString(), "[^0-9]+", string.Empty);

                            var commonPath = Environment.GetEnvironmentVariable(string.Format("VS{0}0COMNTOOLS", vIntString));

                            if (!string.IsNullOrEmpty(commonPath) && !string.IsNullOrWhiteSpace(commonPath))
                            {
                                // Example path here is:
                                // C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\                                
                                int commonPosition = commonPath.IndexOf("\\Common7");

                                if (commonPosition != -1)
                                {
                                    // Find out if the installation directory has the C/C++ compiler present.
                                    // If so, we'll include this version in our returned collection.
                                    commonPath = commonPath.Substring(0, commonPosition).ConvertToHostOsPath();

                                    var vcBinDir = commonPath + Path.DirectorySeparatorChar + "VC" + Path.DirectorySeparatorChar + "bin";

                                    var clPath = vcBinDir + Path.DirectorySeparatorChar + "cl.exe";
                                    if (File.Exists(clPath))
                                    {
                                        installedVersions.Add(toolVer, vcBinDir);
                                    }
                                }
                            }
                        }


                        return installedVersions;
                    }

                    /// <summary>
                    /// Gets a case-insensitive dictionary containing the
                    /// correct environmental variables for the supplied tool
                    /// version and target architecture.
                    /// </summary>
                    /// <returns>
                    /// A dictionary containing the correct environmental
                    /// variables for the supplied tool version and target
                    /// architecture.
                    /// </returns>
                    /// <exception cref="ArgumentException">
                    /// The arch parameter must have one and only one arch flag
                    /// set. In the event that this is not the case, this method
                    /// will throw/
                    /// </exception>
                    private Dictionary<string, string> GetEnvironmentVariables(ToolVersions version, string clBinPath, Architecture arch)
                    {
                        // Ensure we only have one flag for arch.                        
                        int setFlags = 0;
                        foreach (Architecture a in Enum.GetValues(typeof(Architecture)))
                        {
                            if (arch.HasFlag(a))
                            {
                                ++setFlags;
                            }
                        }

                        Debug.Assert(setFlags == 1, "One and only one Architecture flags must be set.");

                        if (setFlags != 1)
                        {
                            throw new ArgumentException("Must have one and only one flag set.", nameof(arch));
                        }

                        // Grab the current environmental variables.
                        var currentEnv = Environment.GetEnvironmentVariables();

                        // Move then into an i-case dictionary.
                        var currentEnvDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var key in currentEnv.Keys)
                        {
                            currentEnvDictionary.Add((string)key, (string)currentEnv[key]);
                        }

                        StringBuilder stdOutput = new StringBuilder();
                        StringBuilder stdErrout = new StringBuilder();

                        // Set it up so we pipe all err and stdout back to us.
                        DataReceivedEventHandler stdOut = ((object sender, DataReceivedEventArgs e) =>
                        {
                            stdOutput.AppendLine(e.Data);
                        });

                        DataReceivedEventHandler stdErr = ((object sender, DataReceivedEventArgs e) =>
                        {
                            stdErrout.AppendLine(e.Data);
                        });

                        // So what we're doing here is we're taking the input tool bin directory, going
                        // up one level. This puts us in the MSVC root directory, where the batch file
                        // used to setup the Visual Studio dev environment always resides. So our command
                        // then invokes this script with the correct target ach params, and calls SET, which
                        // dumps the new/updated environmental variables back out the console standard out,
                        // which we're capturing with our lambda delegates above.
                        var parentDir = Directory.GetParent(clBinPath);
                        var pathToVcars = parentDir.FullName.ConvertToHostOsPath() + Path.DirectorySeparatorChar + "vcvarsall.bat";
                        var archString = string.Empty;

                        // XXX TODO - Need to support cross compiler targets as well. For example,
                        // x86_amd64 to invoke the 32 bit compiler in a way that it outputs 64 bit
                        // binaries, etc.
                        switch (arch)
                        {
                            case Architecture.x86:
                                {
                                    archString = "x86";
                                }
                                break;

                            case Architecture.x64:
                                {
                                    archString = "x64";
                                }
                                break;
                        }

                        // Example of the call string expanded/populated:
                        // call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\vcvarsall.bat" amd64 && SET
                        string callArgs = string.Format("/C call \"{0}\" {1} && SET", pathToVcars, archString);

                        // Once we run cmd.exe with the above args, we'll have captured all environmental
                        // variables required to run the compiler tools manually for the target architecture.
                        var exitCode = RunProcess(WorkingDirectory, "cmd.exe", new List<string> { callArgs }, Timeout.Infinite, null, null, stdErr, stdOut);

                        Debug.Assert(exitCode >= 0, string.Format("When trying to load MSVC dev environment for arch {0}, the process returned an error code.", arch));

                        if (exitCode < 0)
                        {
                            throw new ArgumentException(string.Format("When trying to load MSVC dev environment for arch {0}, the process returned an error code.", arch), nameof(arch));
                        }


                        // Now we should have a bunch of VAR=VALUE params, one per line. So we'll
                        // look for those and try to extract them when we find them.
                        string[] delim = { "\n", "\r" };
                        string[] lines = stdOutput.ToString().Split(delim, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var split = line.Split('=');

                            // Ensure we have split a VAR=VALUE pair, and ensure that they're non-null/empty.
                            // We don't actually care if the value part is empty. It's reasonable/valid
                            // for there to simply be a key present which might have some meaning somewhere.
                            //
                            // XXX TODO - Ensure that keys with empty values don't get disqualified by
                            // the split.Length == 2 qualifier. This shouldn't be the case because we
                            // did not ask for empty results to be pruned from the split operation results.
                            if (split.Length == 2)
                            {

                                if (!string.IsNullOrEmpty(split[0]) && !string.IsNullOrWhiteSpace(split[0]))
                                {
                                    // If this variable already exists, just update/overwrite it.
                                    if (currentEnv.Contains(split[0]))
                                    {
                                        currentEnvDictionary[split[0]] = split[1];
                                    }
                                    else
                                    {
                                        // Variable doesn't exit, so add it.
                                        currentEnvDictionary.Add(split[0], split[1]);
                                    }
                                }
                            }
                        }

                        return currentEnvDictionary;
                    }
                }
            }
        }
    }
}