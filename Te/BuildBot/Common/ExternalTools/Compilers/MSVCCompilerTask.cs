
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BuildBot.Extensions;

namespace BuildBotCore
{
    namespace Common
    {
        namespace ExternalTools
        {
            namespace Compilers
            {
                public class MSVCCompilerTask : AbstractCompilerTask
                {

                    /// <summary>
                    /// Represents various Visual Studio versions where we'll look to invoke
                    /// cl.exe.
                    /// </summary>
                    public enum ToolVersions
                    {
                        /// <summary>
                        /// Visual Studio 2005.
                        /// </summary>
                        v8,

                        /// <summary>
                        /// Visual Studio 2008.
                        /// </summary>
                        v9,

                        /// <summary>
                        /// Visual Studio 2010.
                        /// </summary>      
                        v10,

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
                            throw new NotImplementedException();
                        }
                    }

                    public override List<Exception> Errors
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public override Guid GUID
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public override string Help
                    {
                        get
                        {
                            throw new NotImplementedException();
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
                            throw new NotImplementedException();
                        }
                    }

                    public override string TaskFriendlyName
                    {
                        get
                        {
                            throw new NotImplementedException();
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

                    public override bool Clean()
                    {
                        throw new NotImplementedException();
                    }

                    public override bool Run()
                    {
                        // First thing is to ensure that an acceptable version of MSVC is installed.
                        var installedVcVersions = GetInstalledToolVersions();

                        if(!installedVcVersions.Contains(MinimumRequiredToolVersion))
                        {
                            Errors.Add(new Exception(string.Format("An installation of MSVC meeting the minimum required version of {0} could not be found.", MinimumRequiredToolVersion.ToString())));
                            return false;
                        }

                        return true;
                    }
                    

                    /// <summary>
                    /// Gets a list of all discovered, installed version of
                    /// Visual Studio with Visual C/C++ support.
                    /// </summary>
                    /// <returns>
                    /// A list of all discovered, installed version of Visual
                    /// Studio with Visual C/C++ support.
                    /// </returns>
                    private List<ToolVersions> GetInstalledToolVersions()
                    {
                        
                        var installedVersions = new List<ToolVersions>();

                        foreach(ToolVersions toolVer in Enum.GetValues(typeof(ToolVersions)))
                        {

                            // Check to see if there's an environmental variable for each
                            // version of Visual Studio we list.
                            var vIntString = Regex.Replace(toolVer.ToString(), "[^0-9]+", string.Empty);

                            var commonPath = Environment.GetEnvironmentVariable(string.Format("VS{0}0COMNTOOLS", vIntString));

                            if(!string.IsNullOrEmpty(commonPath) && !string.IsNullOrWhiteSpace(commonPath))
                            {
                                // Example path here is:
                                // C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\                                
                                int commonPosition = commonPath.IndexOf("\\Common7");

                                if(commonPosition != -1)
                                {
                                    // Find out if the installation directory has the C/C++ compiler present.
                                    // If so, we'll include this version in our returned collection.
                                    commonPath = commonPath.Substring(0, commonPosition).ConvertToHostOsPath();

                                    var clPath = commonPath + Path.PathSeparator + "VC" + Path.PathSeparator + "bin" + Path.PathSeparator + "cl.exe";
                                    if(File.Exists(clPath))
                                    {
                                        installedVersions.Add(toolVer);
                                    }
                                }
                            }
                        }

                        return installedVersions;
                    }                    
                }
            }
        }
    }
}