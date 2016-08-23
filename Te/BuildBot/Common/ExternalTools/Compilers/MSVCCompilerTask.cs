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
                    /// Represents various Visual Studio versions where we'll look to invoke
                    /// cl.exe.
                    /// </summary>
                    public enum ToolVersions
                    {
                        /// <summary>
                        /// Visual Studio 2012.
                        /// </summary>      
                        v11,

                        /// <summary>
                        /// Visual Studio 2013.
                        /// </summary>      
                        v12,

                        /// <summary>
                        /// Visual Studio 2015.
                        /// </summary>      
                        v14
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
                            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

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
                        catch(Exception e)
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

                        // Ensure we have sources to compile.
                        Debug.Assert(Sources.Count > 0, "No sources defined.");

                        if (Sources.Count <= 0)
                        {
                            Errors.Add(new Exception("No sources defined."));
                            return false;
                        }

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

                                        // Build intermediary directory
                                        string fullIntermediaryDir = string.Empty;

                                        if(!string.IsNullOrEmpty(IntermediaryDirectory) && !string.IsNullOrWhiteSpace(IntermediaryDirectory))
                                        {
                                            fullIntermediaryDir = IntermediaryDirectory.ConvertToHostOsPath();
                                        }
                                        else
                                        {
                                            // If not specified, use working directory.
                                            fullIntermediaryDir = WorkingDirectory;
                                        }

                                        // Build out so that it's Debug/Release x86/x64
                                        fullIntermediaryDir = Path.PathSeparator + string.Format("{0} {1}", cfg.ToString(), a.ToString());

                                        // Now we need the ENV vars for our tool version.
                                        var envVars = GetEnvironmentVariables(MinimumRequiredToolVersion, installedVcVersions[MinimumRequiredToolVersion], a);

                                        // Set the intermediary output directory.
                                        CompilerFlags.Add(string.Format("/Fo{0}", fullIntermediaryDir + Path.PathSeparator));

                                        switch (a)
                                        {
                                            case Architecture.x86:
                                                {
                                                    LinkerFlags.Add("/MACHINE:x86");
                                                }
                                                break;

                                            case Architecture.x64:
                                                {
                                                    LinkerFlags.Add("/MACHINE:x64");
                                                }
                                                break;
                                        }

                                        // Whether we're merging the link command.
                                        bool mergedLink = true;

                                        switch(OutputAssemblyType)
                                        {
                                            case AssemblyType.Unspecified:
                                            {
                                                Errors.Add(new Exception("No output assembly type specified."));
                                                return false;
                                            }

                                            case AssemblyType.SharedLibrary:
                                            {
                                                // Define User/Win DLL.
                                                CompilerFlags.Add("/D_USRDLL");
                                                CompilerFlags.Add("/D_WINDLL");

                                                LinkerFlags.Add("/DLL");

                                                mergedLink = true;
                                            }
                                            break;

                                            case AssemblyType.StaticLibrary:
                                            {
                                                // /c flag means don't link
                                                if(!CompilerFlags.Contains("/c"))
                                                {
                                                    CompilerFlags.Add("/c");
                                                }
                                            }
                                            break;  

                                            case AssemblyType.Executable:
                                            {
                                                mergedLink = true;
                                            }
                                            break;                                           
                                        }

                                        // Clone.
                                        var finalArgs = new List<string>(CompilerFlags);

                                        // Run compiler.
                                        if(mergedLink)
                                        {
                                            
                                            finalArgs.Add("/link");
                                            finalArgs = finalArgs.Concat(LinkerFlags).ToList();
                                        }


                                        int returnCode = RunProcess(WorkingDirectory, "cl.exe", finalArgs, Timeout.Infinite, envVars);

                                        // CL always returns 0 for success. Any other value is failure:
                                        // Source:
                                        // https://msdn.microsoft.com/en-us/library/ebh0y918.aspx
                                        return returnCode == 0;
                                    }
                                }
                            }                            
                        }                        

                        // Always assume failure first.
                        return false;
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

                                    var vcBinDir = commonPath + Path.PathSeparator + "VC" + Path.PathSeparator + "bin";

                                    var clPath = vcBinDir + Path.PathSeparator + "cl.exe";
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
                    /// Gets A dictionary containing the correct environmental
                    /// variables for the supplied tool version and target
                    /// architecture.
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
                        var currentEnv = (Dictionary<string, string>)Environment.GetEnvironmentVariables();

                        StringBuilder stdOutput = new StringBuilder();
                        StringBuilder stdErrout = new StringBuilder();

                        // Set it up so we pipe all err and stdout back to us.
                        DataReceivedEventHandler stdOut = ((object sender, DataReceivedEventArgs e) =>
                        {
                            stdOutput.Append(e.Data);
                        });

                        DataReceivedEventHandler stdErr = ((object sender, DataReceivedEventArgs e) =>
                        {
                            stdErrout.Append(e.Data);
                        });

                        // So what we're doing here is we're taking the input tool bin directory, going
                        // up one level. This puts us in the MSVC root directory, where the batch file
                        // used to setup the Visual Studio dev environment always resides. So our command
                        // then invokes this script with the correct target ach params, and calls SET, which
                        // dumps the new/updated environmental variables back out the console standard out,
                        // which we're capturing with our lambda delegates above.
                        var parentDir = Directory.GetParent(clBinPath);
                        var pathToVcars = parentDir.FullName.ConvertToHostOsPath() + Path.PathSeparator + "vcvarsall.bat";
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
                        string callArgs = string.Format("/C \"call \"{0}\" {1} && SET", pathToVcars, archString);

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
                        string[] lines = stdOutput.ToString().Split(delim, StringSplitOptions.None);
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
                                    if (currentEnv.ContainsKey(split[0]))
                                    {
                                        currentEnv[split[0]] = split[1];
                                    }
                                    else
                                    {
                                        // Variable doesn't exit, so add it.
                                        currentEnv.Add(split[0], split[1]);
                                    }
                                }
                            }
                        }

                        return currentEnv;
                    }
                }
            }
        }
    }
}