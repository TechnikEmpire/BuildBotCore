
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BuildBotCore
{
    namespace Common
    {
        namespace ExternalTools
        {
            namespace Compilers
            {
                public class GccCompilerTask : AbstractCompilerTask
                {
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
                            bool isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
                            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
                            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

                            return isOsx || isLinux || isWindows;
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
                    /// Constructs a new GccCompilerTask object.
                    /// </summary>
                    /// <param name="scriptAbsolutePath">
                    /// The absolute path to this build task script. This is supplied by the
                    /// master BuildBot application to every build script discovered and
                    /// compiled on instantiation to provide a file system context to the
                    /// build script.
                    /// </param>
                    public GccCompilerTask(string scriptAbsolutePath) : base(scriptAbsolutePath)
                    {

                    }

                    public override bool Clean()
                    {
                        throw new NotImplementedException();
                    }

                    public override bool Run()
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}