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

using System.Threading;
using System.Diagnostics;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Archive;
using SharpCompress.Common;
using System.Net.Http;
using BuildBot.Net.Http.Handlers;
using Microsoft.Extensions.PlatformAbstractions;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using BuildBot.Extensions;
using System.Threading.Tasks;
using System.Net;

namespace BuildBotCore
{

    /// <summary>
    /// Represents compilation target architecture.
    /// </summary>
    [Flags]
    public enum Architecture
    {
        /// <summary>
        /// Use x86 instruction set.
        /// </summary>
        x86 = 1,

        /// <summary>
        /// Use x64 instruction set.
        /// </summary>
        x64 = 2
    }

    /// <summary>
    /// Represents standard Debug and Release compiliation configurations.
    /// </summary>
    [Flags]
    public enum BuildConfiguration
    {
        /// <summary>
        /// Standard debug configuration.
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Standard release configuration.
        /// </summary>
        Release = 2
    }

    /// <summary>
    /// The AbstractBuildTask interface specifies the class members required for
    /// a build task class.
    /// </summary>
    public abstract class AbstractBuildTask
    {

        /// <summary>
        /// Private data member for the public WorkingDirectory property.
        /// </summary>
        private string m_workingDirectory;

        /// <summary>
        /// Gets the supported target architectures for the build task in the
        /// form of enum flags.
        /// </summary>
        /// <remarks>
        /// Given that the Architecture enum has the flags attribute,
        /// it is possible to define more than one target architecture through
        /// this property.
        /// </remarks>
        public abstract Architecture SupportedArchitectures
        {
            get;
        }

        /// <summary>
        /// Gets or sets the target build configuration. The BuildConfiguration
        /// enum flags can be parsed during the execution of the Run() command
        /// so that build task scripts can, if desired, alter their function
        /// based on the supplied flags.
        /// </summary>
        /// <remarks>
        /// Given that the Configuration enum has the flags attribute, it is
        /// possible to define more than one target configuration through this
        /// property.
        /// </remarks>
        public BuildConfiguration Configuration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a bool indicating whether or not the host OS is supported by
        /// this build task.
        /// </summary>        
        public abstract bool IsOsPlatformSupported
        {
            get;
        }

        /// <summary>
        /// Gets a list of unique GUID identifiers that represent external build
        /// tasks. These tasks must be run successfully in order for this build
        /// task to succeed.
        /// </summary>
        public abstract List<Guid> TaskDependencies
        {
            get;
        }

        /// <summary>
        /// Gets a list of exceptions that occurred and were handled by this
        /// build task. If the event that the Run() method returns false, a list
        /// containing one or more exceptions that explain the cause of the
        /// failure. An empty collection otherwise.
        /// </summary>
        /// <remarks>
        /// Build tasks are expected to handle exceptions under all
        /// circumstances, but those exceptions, or custom exceptions that might
        /// provide more clarify on an issue, are expected to be available here
        /// in the event that that the Run() method returns false meaning
        /// failure. If no exception was handled but the task failed for some
        /// non-exception-raising issue, it's expected that at least one
        /// meaningful exception still be returned here.
        /// </remarks>
        public List<Exception> Errors
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a string that should contain meaningful help instructions for
        /// this build task, ideally formatted to display correctly in a typical
        /// console window. A non-string empty string is not required but
        /// strongly suggested.
        /// </summary>
        public abstract string Help
        {
            get;
        }

        /// <summary>
        /// Gets a string that should contain a friendly name for users. This
        /// will be used when displaying information about the task to the user.
        /// When empty, the class name will be used instead.
        /// </summary>
        public abstract string TaskFriendlyName
        {
            get;
        }

        /// <summary>
        /// Gets a unique GUID that identifies this build task. This must be
        /// hard-coded, and cannot be dynamically generated at runtime.
        /// </summary>
        /// <remarks>This cannot be dynamically generated at runtime. If it
        /// were, then this would break a fundamental function of the build
        /// system. Each build task is expected to be unique, and to only
        /// execute once. The hard coded GUID ensures that this requirement is
        /// meant, and that other build tasks can include that hard coded value
        /// their TaskDependencies property.</remarks>
        public abstract Guid GUID
        {
            get;
        }

        /// <summary>
        /// Gets the absolute path on the local file system where this script
        /// exists. This is supplied by the master BuildBot application to every
        /// build script discovered and compiled on instantiation to provide a
        /// file system context to the build script.
        /// </summary>
        public string ScriptAbsolutePath
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the working directory. This is where the
        /// compiler will be invoked from, or if the compiler
        /// supports the concept of a current working directory as
        /// an argument, will be supplied as an argument. If this is
        /// not specified, then the current working directory will
        /// be wherever the host build process has currently set it.
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                return m_workingDirectory;
            }

            set
            {
                m_workingDirectory = value.ConvertToHostOsPath();
            }
        }

        /// <summary>
        /// Constructs a new AbstractBuildTask object.
        /// </summary>
        /// <param name="scriptAbsolutePath">
        /// The absolute path to this build task script. This is supplied by the
        /// master BuildBot application to every build script discovered and
        /// compiled on instantiation to provide a file system context to the
        /// build script.
        /// </param>
        public AbstractBuildTask(string scriptAbsolutePath)
        {
            ScriptAbsolutePath = scriptAbsolutePath;

            // Init non-abstract members.
            Errors = new List<Exception>();

            m_workingDirectory = string.Empty;

            // Initially set the working directory to the parent of
            // the build script folder path. Build scripts are
            // designed to always be in a ".buildbot" path within
            // the project they are meant for. So by getting the
            // parent of the parent of this script path, we should
            // have the main project directory.
            m_workingDirectory = Directory.GetParent(scriptAbsolutePath.ConvertToHostOsPath()).Parent.FullName;
        }

        /// <summary>
        /// Run the clean action for this build task.
        /// </summary>
        /// <returns>
        /// True if the clean operation was a success, false otherwise.
        /// </returns>
        public abstract bool Clean();

        /// <summary>
        /// Run the build task.
        /// </summary>        
        /// <param name="config">
        /// Build configurations requested.
        /// </param>
        /// <param name="arch">
        /// Target architectures requested.
        /// </param>
        /// <returns>
        /// True if the task was a success, false otherwise.
        /// </returns>
        public abstract bool Run(BuildConfiguration config, Architecture arch);

        /// <summary>
        /// Attempts to download a file to the supplied directory, saving either
        /// as the supplied file name or with the file name extracted from the
        /// Content-Disposition header.
        /// </summary>
        /// <param name="source">
        /// The URI source to download from.
        /// </param>
        /// <param name="targetFileName">
        /// The name to use when saving the returned content from the HTTP
        /// request. This can be left blank if you expect the response to
        /// contain Content-Disposition header that includes a "filename="
        /// variable. However, not that if this argument is not supplied and no
        /// "filename=" variable is discovered in the response headers, this
        /// method will throw. See exception details for more. information.
        /// </param>
        /// <param name="targetDirectory">
        /// The target directory where the file will be downloaded and saved to.
        /// Must not be null, empty or whitespace. Must be existing if
        /// forceCreateDirectory is set to false. See exception details for more
        /// information.
        /// </param>
        /// <param name="forceCreateDirectory">
        /// Whether or not to force the creation of the targetDirectory
        /// supplied. This should be left at its default value, which is true.
        /// </param>
        /// <exception cref="targetDirectory">
        /// In the event that the supplied directory is null, empty or
        /// whitespace, this method will throw. In the event that the supplied
        /// directory does not exist, and forceCreateDirectory is explicitly set
        /// to false, this method will throw.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// In the event that no file name is supplied, and one could not be
        /// extracted from the Content-Disposition header in the HTTP response,
        /// this method will throw. Also, basically every operation with Http
        /// can throw this exception, so it should always be handled when
        /// invoking this method.
        /// </exception>
        protected static async Task DownloadFile(string source, string targetDirectory,
            HttpTransactionProgressHandler downloadStatusReportHandler = null,
            string targetFileName = null, bool forceCreateDirectory = true)
        {
            Console.WriteLine(string.Format("Downloading file to {0}", targetDirectory));

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ClientCertificateOptions = ClientCertificateOption.Automatic;
            clientHandler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            targetDirectory = targetDirectory.ConvertToHostOsPath();

            Debug.Assert(!string.IsNullOrEmpty(targetDirectory) && !string.IsNullOrWhiteSpace(targetDirectory), "Supplied target directory cannot be null, empty or whitespace.");

            if (string.IsNullOrEmpty(targetDirectory) || string.IsNullOrWhiteSpace(targetDirectory))
            {
                throw new ArgumentException("Supplied target directory cannot be null, empty or whitespace.", nameof(targetDirectory));
            }

            Debug.Assert(Directory.Exists(targetDirectory) || (!Directory.Exists(targetDirectory) && forceCreateDirectory == true), "Target directory does not exist and forcing its creation was explicitly disallowed.");

            if (!Directory.Exists(targetDirectory))
            {
                if (forceCreateDirectory == false)
                {
                    // This is impossible.
                    throw new ArgumentException("Supplied target directory does not exist, yet force create directory was explicitly set to false.", nameof(targetDirectory));
                }

                Directory.CreateDirectory(targetDirectory);
            }

            // Determine if a file name was supplied to this function or not.
            bool hasFilename = !string.IsNullOrEmpty(targetFileName) && !string.IsNullOrWhiteSpace(targetFileName);

            using (var httpClient = new HttpClient(clientHandler))
            {
                // XXX TODO - Add accept/user agent headers?

                // Get the response with headers read in, but not the content.
                var responseMessage = await httpClient.GetAsync(source, HttpCompletionOption.ResponseHeadersRead);

                // If we don't have a supplied filename for the download, we need to extract it from headers.
                if (!hasFilename)
                {
                    if (responseMessage.Content.Headers.ContentDisposition != null)
                    {
                        var dFileName = responseMessage.Content.Headers.ContentDisposition.FileName;
                        if (!string.IsNullOrEmpty(dFileName) && !string.IsNullOrWhiteSpace(dFileName))
                        {
                            targetFileName = dFileName;
                            hasFilename = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine("No content disposition header.");
                    }
                }

                Debug.Assert(hasFilename, "A file name for the download was not supplied and none could be extracted from the response headers.");

                if (!hasFilename)
                {
                    throw new HttpRequestException("A file name for the download was not supplied and none could be extracted from the response headers.");
                }

                // If the Content-Length header is available. For reporting.
                ulong totalContentLength = 0;
                if (responseMessage.Content.Headers.ContentLength.HasValue)
                {
                    totalContentLength = (ulong)responseMessage.Content.Headers.ContentLength.Value;
                }

                ulong totalBytesRead = 0;

                if (downloadStatusReportHandler == null)
                {
                    downloadStatusReportHandler = (received, total) =>
                    {
                        if (total > 0)
                        {
                            Console.Write("\rDownload progress: {0}%", Math.Round(((float)received / (float)total) * 100, 2));                            
                        }
                        else
                        {
                            Console.Write("\rDownloaded {0} bytes of unknown.", received);
                        }
                    };
                }               

                Console.WriteLine(string.Format("Full download location: {0}", targetDirectory + Path.DirectorySeparatorChar + targetFileName));

                using (var destinationFileStream = File.Create(targetDirectory + Path.DirectorySeparatorChar + targetFileName))
                {
                    using (var stream = await responseMessage.Content.ReadAsStreamAsync())
                    {
                        byte[] buffer = new byte[2048];
                        int bytesRead;
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            // In total bytes read.
                            totalBytesRead += (ulong)bytesRead;

                            await destinationFileStream.WriteAsync(buffer, 0, bytesRead);

                            // Report status
                            downloadStatusReportHandler?.Invoke(totalBytesRead, totalContentLength);
                        }
                    }
                }
            }

            // Since we may have been writing progress data to
            // the console, force a newline here.
            Console.WriteLine();
        }

        /// <summary>
        /// Attempts to decompress the archive at the given file path to the
        /// supplied decompression path.
        /// </summary>
        /// <param name="archivePath">
        /// The path to the archive to decompress.
        /// </param>
        /// <param name="decompressedPath">
        /// The path to expand the archive into.
        /// </param>
        /// <param name="verbose">
        /// Whether or not to print information about the extraction to the
        /// console.
        /// </param>
        /// <param name="forceCreateDirectory">
        /// Whether or not to force the creation of the target decompression
        /// path. This should be left at the default value, which is true.
        /// </param>
        /// <remarks>
        /// May throw exceptions related to file IO or path formatting
        /// exceptions. This method is not designed to automatically handle
        /// exception, as documented in this class, it is the up to the user to
        /// handle and return exceptions via the appropriate exposed properties
        /// or methods.
        /// </remarks>
        /// <exception cref="FileNotFoundException">
        /// In the event that the file pointed to by the supplied archive path
        /// does not exist, this method will throw.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied archive file path is null, empty or
        /// whitespace, this method will throw. In the event that the supplied
        /// decompressedPath argument is null, empty or whitespace, this method
        /// will throw. In the event that the decompressedPath points to a
        /// directory that does not exist, but forceCreateDirectory was
        /// explicitly set to false, this method will throw.
        /// </exception>
        protected static void DecompressArchive(string archivePath,
            string decompressedPath, bool verbose = false, bool forceCreateDirectory = true)
        {
            // Enforce create directory if specified.
            if (forceCreateDirectory)
            {
                if (!Directory.Exists(decompressedPath))
                {
                    Directory.CreateDirectory(decompressedPath);
                }
            }

            Debug.Assert(!string.IsNullOrEmpty(archivePath) && !string.IsNullOrWhiteSpace(archivePath), "Supplied archivePath is null, empty or whitespace.");

            Debug.Assert(!string.IsNullOrEmpty(decompressedPath) && !string.IsNullOrWhiteSpace(decompressedPath), "Supplied decompressedPath is null, empty or whitespace.");

            Debug.Assert(File.Exists(archivePath), string.Format("Supplied archivePath points to a file that does not exist: {0}.", archivePath));

            Debug.Assert(Directory.Exists(decompressedPath) || (!Directory.Exists(decompressedPath) && forceCreateDirectory == true), "Decompression directory does not exist and forcing its creation was explicitly disallowed.");

            // Can't extract nothingness.
            if (!File.Exists(archivePath))
            {
                throw new ArgumentException(string.Format("Supplied archivePath points to a file that does not exist: {0}.", archivePath), nameof(archivePath));
            }

            // Can't extract to nowhere, especially nowhere we can't create.
            if (!Directory.Exists(decompressedPath) && forceCreateDirectory == false)
            {
                throw new ArgumentException("Supplied decompression directory does not exist, yet force create directory was explicitly set to false.", nameof(decompressedPath));
            }

            var archive = ArchiveFactory.Open(archivePath);

            Console.WriteLine(string.Format("Decompressing archive {0} ...", Path.GetFileName(archivePath)));

            int current = 0;
            int total = archive.Entries.Count();
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory)
                {
                    ++current;
                    if(verbose)
                    {
                        Console.WriteLine(string.Format("Extracting file: {0}", (decompressedPath + Path.DirectorySeparatorChar + entry.Key).ConvertToHostOsPath()));
                    }
                    else
                    {
                        Console.Write("\rDecompression progress: {0}%", Math.Round(((float)current / (float)total) * 100, 2));
                    }
                    
                    entry.WriteToDirectory(decompressedPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                }
            }

            // Clear the previous overwritten line.
            Console.WriteLine();
        }

        /// <summary>
        /// Runs the given process with arguments and waits for the process to
        /// exit. This is a convenience function for running simple external
        /// tasks, but a lot of configuration capability is provided via optional
        /// function arguments.
        /// </summary>
        /// <param name="workingDirectory">
        /// The working directory from which to run the process.
        /// </param>
        /// <param name="processName">
        /// The name of the binary to execute.
        /// </param>
        /// <param name="processArgs">
        /// Arguments to pass to the binary upon execution.
        /// </param>
        /// <param name="environment">
        /// Environmental variables to set for the binary to be executed.
        /// </param>
        /// <param name="processPath">
        /// The path to the binary to execute.
        /// </param>
        /// <remarks>
        /// May throw exceptions related to file IO or path formatting
        /// exceptions. This method is not designed to automatically handle
        /// exception, as documented in this class, it is the up to the user to
        /// handle and return exceptions via the appropriate exposed properties
        /// or methods.
        ///
        /// This method always waits for the spawned process to exit.
        /// </remarks>        
        /// <param name="onStandardError">
        /// Callback for receiving standard error data from the process. If this
        /// callback is not supplied, it will be redirected to the parent
        /// console.
        /// </param>
        /// <param name="onStandardOut">
        /// Callback for receiving standard output data from the process. If
        /// this callback is not supplied, it will be redirected to the parent
        /// console.
        /// </param>
        /// <returns>
        /// The exit code returned by the executed binary.
        /// </returns>
        protected static int RunProcess(string workingDirectory, string processName,
            List<string> processArgs, int waitMillisections = Timeout.Infinite,
            IDictionary<string, string> environment = null,
            string processPath = null, DataReceivedEventHandler onStandardError = null,
            DataReceivedEventHandler onStandardOut = null)
        {
            // The param being  non-null is insufficient to deem it valid.
            bool procPathExists = false;

            // Cleanup process path if exists
            if (!string.IsNullOrEmpty(processPath) && !string.IsNullOrWhiteSpace(processPath))
            {
                if (processPath.Length > 0)
                {
                    procPathExists = true;

                    // Ensure path separator is proper for the environment and no trailing separators.
                    processPath = processPath.ConvertToHostOsPath();
                }
            }

            Process p = new Process();
            p.StartInfo.Arguments = string.Join(" ", processArgs);
            p.StartInfo.WorkingDirectory = workingDirectory;
            p.StartInfo.FileName = procPathExists == false ? processName : processPath + Path.DirectorySeparatorChar + processName;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;

            if (environment != null)
            {
                foreach (var key in environment.Keys)
                {
                    if (!p.StartInfo.Environment.ContainsKey(key))
                    {
                        p.StartInfo.Environment.Add(key, environment[key]);
                    }
                    else
                    {
                        // Overwrite if exists.
                        p.StartInfo.Environment[key] = environment[key];
                    }
                }
            }

            foreach (var kp in p.StartInfo.Environment)
            {
                //Console.WriteLine(string.Format("{0} ::: {1}", kp.Key, kp.Value));
            }

            if (onStandardError != null)
            {
                p.ErrorDataReceived += onStandardError;
            }
            else
            {
                p.ErrorDataReceived += ((sender, e) =>
                {
                    Console.WriteLine(e.Data);
                });
            }

            if (onStandardOut != null)
            {
                p.OutputDataReceived += onStandardOut;
            }
            else
            {
                p.OutputDataReceived += ((sender, e) =>
                {
                    Console.WriteLine(e.Data);
                });
            }

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            if (waitMillisections != Timeout.Infinite)
            {
                p.WaitForExit(waitMillisections);
            }
            else
            {
                p.WaitForExit();
            }

            p.CancelOutputRead();
            p.CancelErrorRead();

            return p.ExitCode;
        }

        /// <summary>
        /// Copies the contents of one directory to another location. The
        /// destination location will be created if it does not already exist.
        /// </summary>
        /// <param name="sourceDirectory">
        /// The source directory from which to copy contents.
        /// </param>
        /// <param name="destinationDirectory">
        /// The destination directory to copy the contents of the source
        /// directory to.
        /// </param>
        /// <param name="recurive">
        /// Whether or not the operation is recursive.
        /// </param>
        /// <param name="included">
        /// A list of file names file extensions to be included in the copy.
        /// Optional. Default is null. If non-null, the exclusion list will
        /// override this. Also if non-null, and a file or its extension cannot
        /// be found within this list, then it will not be copied regardless of
        /// the exclusion list.
        /// </param>
        /// <param name="excluded">
        /// A list of file names file extensions to be excluded in the copy.
        /// Optional. Default is null.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the supplied source directory does not exist, this
        /// method will throw.
        /// </exception>     
        protected static void CopyDirectory(string sourceDirectory, string destinationDirectory,
            bool recurive, bool overwrite = true, HashSet<string> included = null, HashSet<string> excluded = null)
        {
            if (excluded == null)
            {
                excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirectory);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(string.Format("Source does not exist: {0}", sourceDirectory));
            }

            // Force destination directory creation.
            Directory.CreateDirectory(destinationDirectory);

            // Get all files in the source directory and copy them.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                // If files are not excluded, check to see if inclusion list
                // exists. If it does exist and the file extension or name isn't
                // on the list, then don't copy. If it doesn't exist, just copy.
                if (!excluded.Contains(file.Extension) && !excluded.Contains(file.Name))
                {
                    if (included != null && (!included.Contains(file.Name) && !included.Contains(file.Extension)))
                    {
                        continue;
                    }

                    string temppath = Path.Combine(destinationDirectory, file.Name);
                    file.CopyTo(temppath, overwrite);
                }
            }

            // Repeat for all subdirs if recursive.
            if (recurive)
            {
                DirectoryInfo[] subDirectories = dir.GetDirectories();

                foreach (DirectoryInfo subdir in subDirectories)
                {
                    string temppath = Path.Combine(destinationDirectory, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath, recurive);
                }
            }
        }

        /// <summary>
        /// Checks whether or not the file at the supplied path matches the
        /// expected hash.
        /// </summary>
        /// <param name="algorithmName">
        /// The name of the algorithm to use.
        /// </param>
        /// <param name="filePath">
        /// The path to the file to hash.
        /// </param>
        /// <param name="expectedHash">
        /// The expected hash of the file at the supplied path.
        /// </param>
        /// <returns>
        /// True if the file was found and it's hash matched the expected value.
        /// False otherwise. False also may be an indication that the file
        /// simply did not exist.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// In the event that algorithmName is null, this method will throw.
        ///
        /// In the event that the algorithm identified by the given name is not
        /// supported, this method will throw.
        ///
        /// In the event that either the filePath or expectedHash arguments are
        /// null, whitespace or empty strings, this method will throw.
        /// </exception>
        public static bool VerifyFileHash(HashAlgorithmName algorithmName, string filePath, string expectedHash)
        {

            Debug.Assert(algorithmName != null, "Supplied HashAlgorithmName cannot be null.");

            Debug.Assert(!string.IsNullOrEmpty(filePath) && !string.IsNullOrWhiteSpace(filePath), "Supplied file path cannot be null, empty or whitespace.");

            Debug.Assert(!string.IsNullOrEmpty(expectedHash) && !string.IsNullOrWhiteSpace(expectedHash), "Supplied hash cannot be null, empty or whitespace.");

            if(algorithmName == null)
            {
                throw new ArgumentException("Supplied HashAlgorithmName cannot be null.", nameof(algorithmName));
            }

            if (string.IsNullOrEmpty(filePath) || string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Supplied file path cannot be null, empty or whitespace.", nameof(filePath));
            }

            if (string.IsNullOrEmpty(expectedHash) || string.IsNullOrWhiteSpace(expectedHash))
            {
                throw new ArgumentException("Supplied hash cannot be null, empty or whitespace.", nameof(expectedHash));
            }

            // Just return false if the file doesn't exist.
            if (!File.Exists(filePath))
            {
                return false;
            }

            HashAlgorithm algo = null;

            try
            {
                switch (algorithmName.Name)
                {
                    case nameof(HashAlgorithmName.MD5):
                        {
                            algo = MD5.Create();
                        }
                        break;

                    case nameof(HashAlgorithmName.SHA1):
                        {
                            algo = SHA1.Create();
                        }
                        break;

                    case nameof(HashAlgorithmName.SHA256):
                        {
                            algo = SHA256.Create();
                        }
                        break;

                    case nameof(HashAlgorithmName.SHA384):
                        {
                            algo = SHA384.Create();
                        }
                        break;

                    case nameof(HashAlgorithmName.SHA512):
                        {
                            algo = SHA512.Create();
                        }
                        break;

                    default:
                        throw new ArgumentException("Specified hash algorithm is unsupported.", nameof(algorithmName));
                }

                algo.Initialize();

                using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var hash = BitConverter.ToString(algo.ComputeHash(file)).Replace("-","");

                    return hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
                }
            }
            finally
            {
                if (algo != null)
                {
                    algo.Dispose();
                }
            }
        }

        /// <summary>
        /// Writes the given string to the console, followed by a new line.
        /// </summary>
        /// <param name="message">
        /// The text to write to the console.
        /// </param>
        /// <remarks>
        /// These methods are temporary until the type collisions caused by the
        /// inclusion of the private mscorlib in dynamic script compilation are
        /// resolved alternative. XXX TODO.
        /// </remarks>
        public static void WriteLineToConsole(string message)
        {
            WriteToConsole(message);
            WriteToConsole(Environment.NewLine);
        }

        /// <summary>
        /// Writes the given string to the console.
        /// </summary>
        /// <param name="message">
        /// The text to write to the console.
        /// </param>
        /// <remarks>
        /// These methods are temporary until the type collisions caused by the
        /// inclusion of the private mscorlib in dynamic script compilation are
        /// resolved alternative. XXX TODO.
        /// </remarks>
        public static void WriteToConsole(string message)
        {
            Console.Write(message);
        }
    }
}