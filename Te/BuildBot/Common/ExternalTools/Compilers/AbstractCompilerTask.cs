
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

                /// <summary>
                /// An enum of all possible assembly types that can be
                /// generated.
                /// </summary>
                public enum AssemblyType
                {
                    /// <summary>
                    /// Not specified. Default value.
                    /// </summary>
                    Unspecified,
                    
                    /// <summary>
                    /// Generate a shared library.
                    /// </summary>
                    SharedLibrary,

                    /// <summary>
                    /// Generate a static library.
                    /// </summary>
                    StaticLibrary,

                    /// <summary>
                    /// Generate an executable.
                    /// </summary>
                    Executable
                }

                /// <summary>
                /// The AbstractCompilerTask class provides properties and
                /// functionality that are common to all compliation tasks. All
                /// compilers have in the common the need for sources, temp
                /// directories for intermediaries, include directories, library
                /// paths and so on. It is the goal of this task to support such
                /// properties and associated functionality here.
                /// </summary>
                /// <remarks>
                /// This class must remain abstract by nature in order to
                /// delegate implementation of AbstractBuildTask.
                /// </remarks>
                public abstract class AbstractCompilerTask : AbstractBuildTask
                {
                    /// <summary>
                    /// Private data member for the public IncludePaths property.
                    /// </summary>
                    private List<string> m_includePaths;

                    /// <summary>
                    /// Private data member for the public CompilerFlags property.
                    /// </summary>
                    private List<string> m_compilerFlags;

                    /// <summary>
                    /// Private data member for the public LinkerFlags property.
                    /// </summary>
                    private List<string> m_linkerFlags;

                    /// <summary>
                    /// Private data member for the public IntermediaryDirectory property.
                    /// </summary>
                    private string m_intermediaryDirectory;

                    /// <summary>
                    /// Private data member for the public AdditionalLibraries property.
                    /// </summary>
                    private List<string> m_additionalLibraries;


                    /// <summary>
                    /// Private data member for the public LibraryPaths property.
                    /// </summary>
                    private List<string> m_libraryPaths;                    

                    /// <summary>
                    /// Private data member for the public Sources property.
                    /// </summary>
                    private List<string> m_sources;

                    /// <summary>
                    /// Gets or sets whether or not to verify that supplied
                    /// include paths, directory paths and source file paths
                    /// exist.
                    /// </summary>
                    /// <remarks>
                    /// If supplied, the parent for the IntermediaryDirectory
                    /// path is verified, but IntermediaryDirectory is not
                    /// verified as it is created on demand.
                    /// Defaults to false.
                    /// </remarks>                    
                    public bool StrictPaths
                    {
                        get;
                        set;
                    }                    

                    /// <summary>
                    /// 
                    /// </summary>
                    /// <returns></returns>
                    public List<string> Sources
                    {
                        get
                        {
                            return m_sources;
                        }

                        set
                        {
                            m_sources = value;

                            // Silently enforce non-null, but rather empty.
                            if (m_sources == null)
                            {
                                m_sources = new List<string>();
                            }

                            // If StrictPaths is true, we need to verify supplied paths.
                            if (StrictPaths)
                            {
                                if (m_sources.Count > 0)
                                {
                                    foreach (var entry in m_sources)
                                    {
                                        var saneEntry = entry.ConvertToHostOsPath();

                                        // Determine if this is just a file name or has a parent dir specified.                                    
                                        var fName = Path.GetFileName(saneEntry);

                                        if(!fName.Equals(saneEntry, StringComparison.OrdinalIgnoreCase))
                                        {
                                            // This means there must be directory info in our sane entry.
                                            // This must be true because our retrieved file name does not
                                            // match the entry name.
                                            //
                                            // As such, we simply check to see if the file exists.
                                            if (!File.Exists(saneEntry) && !File.Exists(Path.GetFullPath(saneEntry)))
                                            {
                                                throw new FileNotFoundException(string.Format("Supplied source file could not be found: {0}.", saneEntry));
                                            }
                                        }
                                        else
                                        {
                                            // There's no path information, so we'll check to see if the
                                            // file is inside an included, relevant directory.
                                            if (string.IsNullOrEmpty(WorkingDirectory) || string.IsNullOrWhiteSpace(WorkingDirectory))
                                            {
                                                throw new FileNotFoundException(string.Format(@"Supplied source file could not be found: {0}. 
                                                Could not be found because the path is not absolute and the working directory is not specified
                                                so there is nothing to expand these relative paths against.", saneEntry));
                                            }

                                            bool foundSourceRelative = false;

                                            string expandedPath = (WorkingDirectory + Path.DirectorySeparatorChar + saneEntry);

                                            if (File.Exists(expandedPath))
                                            {
                                                var libFileAttributes = File.GetAttributes(expandedPath);

                                                if (!libFileAttributes.HasFlag(FileAttributes.Directory))
                                                {
                                                    foundSourceRelative = true;
                                                    break;
                                                }
                                            }

                                            if (!foundSourceRelative)
                                            {
                                                throw new FileNotFoundException(string.Format("Supplied library could not be found: {0}.", saneEntry));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// Gets or sets a list of libraries to supply to the
                    /// compiler during compilation. When using StrictPaths,
                    /// ensure that the LibraryPaths property is set before this
                    /// property. See exception remarks for more details.
                    /// </summary>
                    /// <exception cref="FileNotFoundException">
                    /// If StrictPaths is set to true, the existence of each
                    /// supplied library is verified. If the supplied libraries
                    /// cannot be found by checking the full path of each
                    /// library, then this setter will throw. Typically
                    /// libraries are supplied without a full path. In this
                    /// case, the AdditionalLibraries paths are enumerated in
                    /// conjunction with the supplied library names to check if
                    /// they exist.
                    ///
                    /// Due to the nature of this check, when using StrictPaths,
                    /// ensure that you set the LibraryPaths property before
                    /// setting this.
                    /// </exception>
                    /// <exception cref="DirectoryNotFoundException">
                    /// When using StrictPaths, the LibraryPaths property will
                    /// be expanded to search for libraries that are not given
                    /// with an absolute path. As such, the LibraryPaths and all
                    /// contained directories verified as well, which may case
                    /// this setter to throw a DirectoryNotFoundException.
                    /// </exception>
                    /// <exception cref="ArgumentException">
                    /// When using StrictPaths, this setter will throw when any
                    /// supplied path contains invalid characters.
                    /// </exception>
                    public List<string> AdditionalLibraries
                    {
                        get
                        {
                            return m_additionalLibraries;
                        }

                        set
                        {
                            m_additionalLibraries = value;

                            // Silently enforce non-null, but rather empty.
                            if (m_additionalLibraries == null)
                            {
                                m_additionalLibraries = new List<string>();
                            }

                            // If StrictPaths is true, we need to verify supplied paths.
                            if (StrictPaths)
                            {
                                if (m_additionalLibraries.Count > 0)
                                {
                                    foreach (var entry in m_additionalLibraries)
                                    {
                                        var saneEntry = entry.ConvertToHostOsPath();

                                        // Determine if this is just a file name or has a parent dir specified.                                    
                                        var fName = Path.GetFileName(saneEntry);

                                        if(!fName.Equals(saneEntry, StringComparison.OrdinalIgnoreCase))
                                        {
                                            // This means there must be directory info in our sane entry.
                                            // This must be true because our retrieved file name does not
                                            // match the entry name.
                                            //
                                            // As such, we simply check to see if the file exists.
                                            if (!File.Exists(saneEntry) && !File.Exists(Path.GetFullPath(saneEntry)))
                                            {
                                                throw new FileNotFoundException(string.Format("Supplied library could not be found: {0}.", entry));
                                            }                                           
                                        }
                                        else
                                        {
                                            if (LibraryPaths.Count == 0)
                                            {
                                                throw new FileNotFoundException(string.Format(@"Supplied library could not be found: {0}. 
                                                Could not be found because the path is not absolute and no library directories have been 
                                                supplied. When using StrictPaths, ensure that library directories are configured before 
                                                configuring the libraries property.", entry));
                                            }

                                            bool foundLibInLibPath = false;

                                            // We'll use a little cheat here to re-validate the LibraryPaths
                                            // before moving on. Get and then set, invoking the setter will
                                            // re-run validation.
                                            var currentLibPaths = LibraryPaths;
                                            if (currentLibPaths.Count > 0)
                                            {
                                                LibraryPaths = currentLibPaths;
                                            }

                                            // Not an absolute path, so we need to expand supplied
                                            // library directories paths and check those.
                                            foreach (var libPathEntry in LibraryPaths)
                                            {
                                                string expandedPath = (libPathEntry + Path.DirectorySeparatorChar + saneEntry);

                                                if (File.Exists(expandedPath))
                                                {
                                                    var libFileAttributes = File.GetAttributes(expandedPath);

                                                    if (!libFileAttributes.HasFlag(FileAttributes.Directory))
                                                    {
                                                        foundLibInLibPath = true;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (!foundLibInLibPath)
                                            {
                                                throw new FileNotFoundException(string.Format("Supplied library could not be found: {0}.", saneEntry));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// Gets or sets include directories to be supplied to the
                    /// compiler during compilation.
                    /// </summary>  
                    /// <exception cref="DirectoryNotFoundException">
                    /// In the event that StrictPaths is true, and one or more
                    /// directory paths supplied here do not exist, then the
                    /// setter for this property will throw.
                    /// </exception>    
                    /// <exception cref="ArgumentException">
                    /// When using StrictPaths, this setter will throw when any
                    /// supplied path contains invalid characters.
                    /// </exception>              
                    public List<string> IncludePaths
                    {
                        get
                        {
                            return m_includePaths;
                        }

                        set
                        {
                            m_includePaths = value;

                            // Silently enforce non-null, but rather empty.
                            if (m_includePaths == null)
                            {
                                m_includePaths = new List<string>();
                            }

                            // Ensure path separator in entries is proper for the environment.
                            for (var i = 0; i < m_includePaths.Count; ++i)
                            {
                                // Ensure path separator is proper for the environment and no trailing separators.
                                m_includePaths[i] = m_includePaths[i].ConvertToHostOsPath();
                            }

                            // If StrictPaths is true, we need to verify supplied paths.
                            if (StrictPaths)
                            {
                                if (m_includePaths.Count > 0)
                                {
                                    foreach (var entry in m_includePaths)
                                    {
                                        // Check if path exists.
                                        if (!Directory.Exists(entry))
                                        {
                                            throw new DirectoryNotFoundException(string.Format("Supplied include path does not exist: {0}.", entry));
                                        }

                                        // Path/file name validity needs to be
                                        // checked AFTER we've ensure that they
                                        // are valid directories or files. We
                                        // will get false results here if we do
                                        // not ensure this.

                                        var fileAttributes = File.GetAttributes(entry);

                                        if (!fileAttributes.HasFlag(FileAttributes.Directory))
                                        {
                                            throw new ArgumentException(string.Format("Supplied include path does not point to a directory: {0}", entry), nameof(IncludePaths));
                                        }

                                        // Check if path contains invalid characters
                                        if (entry.ContainsInvalidPathCharacters())
                                        {
                                            throw new ArgumentException(string.Format("Supplied include path contains illegal path characters: {0}", entry), nameof(IncludePaths));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// Gets or sets the output assembly type. Defaults to
                    /// Unspecified. Must be set manually or the Run() command
                    /// will throw and fail.
                    /// </summary>
                    public AssemblyType OutputAssemblyType
                    {
                        get;
                        set;
                    }

                    /// <summary>
                    /// Gets or sets flags that should be supplied to the
                    /// compiler during compilation.
                    /// </summary>
                    public List<string> CompilerFlags
                    {
                        get
                        {
                            return m_compilerFlags;
                        }

                        set
                        {
                            m_compilerFlags = value;

                            // Silently enforce non-null, but rather empty.
                            if (m_compilerFlags == null)
                            {
                                m_compilerFlags = new List<string>();
                            }
                        }
                    }

                    /// <summary>
                    /// Gets or sets flags that should be supplied to the
                    /// compiler during compilation.
                    /// </summary>
                    public List<string> LinkerFlags
                    {
                        get
                        {
                            return m_linkerFlags;
                        }

                        set
                        {
                            m_linkerFlags = value;

                            // Silently enforce non-null, but rather empty.
                            if (m_linkerFlags == null)
                            {
                                m_linkerFlags = new List<string>();
                            }
                        }
                    }

                    /// <summary>
                    /// Gets or sets the directory where intermediaries
                    /// generated during compilation should be stored.
                    /// </summary>
                    /// <remarks>
                    /// This directory must be an absolute path, even with
                    /// StrictPaths off. The reason for this requirement is
                    /// because on clean operations, this directory has all
                    /// contents deleted. Therefore, so ensure BuildBot cannot
                    /// ever be blamed for deleting someones precious data, this
                    /// must be an absolute path.
                    /// </remarks>
                    /// <exception cref="DirectoryNotFoundException">
                    /// In the event that StrictPaths is true, and the parent
                    /// directory for the supplied intermediary directory does
                    /// not exist, then the setter for this property will throw.
                    /// </exception>
                    /// <exception cref="ArgumentException">
                    /// In the event that the supplied path is not absolute,
                    /// this method will throw.
                    /// </exception>
                    public string IntermediaryDirectory
                    {
                        get
                        {
                            return m_intermediaryDirectory;
                        }

                        set
                        {
                            m_intermediaryDirectory = value;

                            // Silently enforce non-null, but rather empty.
                            if (m_intermediaryDirectory == null)
                            {
                                m_intermediaryDirectory = string.Empty;
                            }

                            // Ensure path separator is proper for the environment and no trailing separators.
                            m_intermediaryDirectory = m_intermediaryDirectory.ConvertToHostOsPath();

                            string fullPath = Path.GetFullPath(m_intermediaryDirectory).ConvertToHostOsPath();

                            bool isAbsPath = false;

                            isAbsPath = fullPath.Equals(m_intermediaryDirectory, StringComparison.OrdinalIgnoreCase);

                            // Ensure that this is an absolute path.
                            Debug.Assert(isAbsPath, "Must be an absolute path.");

                            if (!isAbsPath)
                            {
                                throw new ArgumentException("Must be an absolute path.", nameof(IntermediaryDirectory));
                            }

                            if (StrictPaths)
                            {
                                if (!string.IsNullOrEmpty(m_intermediaryDirectory) && !string.IsNullOrWhiteSpace(m_intermediaryDirectory))
                                {
                                    // This is enough. If the parent doesn't exit, it'll throw.
                                    var parent = Directory.GetParent(m_intermediaryDirectory);
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// Gets or sets library directories to be supplied to the
                    /// compiler during compilation.
                    /// </summary>  
                    /// <exception cref="DirectoryNotFoundException">
                    /// In the event that StrictPaths is true, and one or more
                    /// directory paths supplied here do not exist, then the
                    /// setter for this property will throw.
                    /// </exception>                    
                    public List<string> LibraryPaths
                    {
                        get
                        {
                            return m_libraryPaths;
                        }

                        set
                        {
                            m_libraryPaths = value;

                            // Silently enforce non-null, but rather empty.
                            if (m_libraryPaths == null)
                            {
                                m_libraryPaths = new List<string>();
                            }

                            // Ensure path separator in entries is proper for the environment.
                            for (var i = 0; i < m_libraryPaths.Count; ++i)
                            {
                                // Ensure path separator is proper for the environment and no trailing separators.
                                m_libraryPaths[i] = m_libraryPaths[i].ConvertToHostOsPath();
                            }

                            // If StrictPaths is true, we need to verify supplied paths.
                            if (StrictPaths)
                            {
                                if (m_libraryPaths.Count > 0)
                                {
                                    foreach (var entry in m_libraryPaths)
                                    {
                                        if (!Directory.Exists(entry))
                                        {
                                            throw new DirectoryNotFoundException(string.Format("Supplied library include path does not exist: {0}.", entry));
                                        }


                                        // Path/file name validity needs to be
                                        // checked AFTER we've ensure that they
                                        // are valid directories or files. We
                                        // will get false results here if we do
                                        // not ensure this.
                                        var fileAttributes = File.GetAttributes(entry);

                                        if (!fileAttributes.HasFlag(FileAttributes.Directory))
                                        {
                                            throw new ArgumentException(string.Format("Supplied library include path does not point to a directory: {0}", entry), nameof(LibraryPaths));
                                        }

                                        // Check if path contains invalid characters
                                        if (entry.ContainsInvalidPathCharacters())
                                        {
                                            throw new ArgumentException(string.Format("Supplied library include path contains illegal path characters: {0}", entry), nameof(LibraryPaths));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    /// <summary>
                    /// Constructs a new AbstractCompilerTask object.
                    /// </summary>
                    /// <param name="scriptAbsolutePath">
                    /// The absolute path to this build task script. This is supplied by the
                    /// master BuildBot application to every build script discovered and
                    /// compiled on instantiation to provide a file system context to the
                    /// build script.
                    /// </param>
                    public AbstractCompilerTask(string scriptAbsolutePath) : base(scriptAbsolutePath)
                    {
                        // Init private property data holders.
                        m_compilerFlags = new List<string>();
                        m_includePaths = new List<string>();
                        m_intermediaryDirectory = string.Empty;
                        m_libraryPaths = new List<string>();                        
                        m_additionalLibraries = new List<string>();

                        // Default string paths to false.
                        StrictPaths = false;                        
                    }
                }
            }
        }
    }
}