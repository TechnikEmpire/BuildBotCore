
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
                            // XXX TODO - This needs to be done properly. We can't simply assume
                            // that we can support x86 and x64. This needs to be checked within
                            // this class based on the version and flavour of GCC detected, rather
                            // than simply returning static values.
                            throw new NotImplementedException();
                        }
                    }

                    public override Guid GUID
                    {
                        get
                        {
                            return Guid.Parse("82b3c291-11d4-4f91-ac09-f82438f020be");
                        }
                    }

                    public override string Help
                    {
                        get
                        {
                            return "The GccCompilerTask is meant to be used internally by another build task.";
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
                            // Depends on nothing.
                            return new List<Guid>();
                        }
                    }

                    public override string TaskFriendlyName
                    {
                        get
                        {
                            return "GCC Compiler Task";
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

                    public override bool Run(BuildConfiguration config, Architecture arch)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}