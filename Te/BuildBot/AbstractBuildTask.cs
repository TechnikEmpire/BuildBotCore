using System.Threading;
using System.Diagnostics;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Archive;
using SharpCompress.Common;

namespace BuildBotCore
{

    /// <summary>
    /// Represents compilation target architecture.
    /// </summary>
    [Flags]
    public enum Architecture
    {
        x86,
        x64
    }

    /// <summary>
    /// The AbstractBuildTask interface specifies the class members required for
    /// a build task class.
    /// </summary>
    public abstract class AbstractBuildTask
    {

        public AbstractBuildTask()
        {

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
        /// <returns>
        /// True if the task was a success, false otherwise.
        /// </returns>
        public abstract bool Run();

        /// <summary>
        /// Gets the supported target architectures for the build task.
        /// </summary>
        /// <remarks>Given that the Architecture enum has the flags attribute,
        /// it is possible to define more than one target architecture through
        /// this property.</remarks>
        /// <returns>
        /// Enum flags representing all architectures supported by the build
        /// task.
        /// </returns>
        public abstract Architecture Architectures
        {
            get;
        }

        /// <summary>
        /// Gets a list of unique GUID identifiers that represent external build
        /// tasks. These tasks must be run successfully in order for this build
        /// task to succeed.
        /// </summary>
        /// <returns>
        /// A list of unique GUID identifiers representing external build tasks
        /// hat must have been run successfully in order for this build task to
        /// succeed.
        /// </returns>
        public abstract List<Guid> TaskDependencies
        {
            get;
        }

        /// <summary>
        /// Gets a list of exceptions that occurred and were handled by this
        /// build task.
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
        /// <returns>
        /// If the event that the Run() method returns false, meaning failure, a
        /// list containing one or more exceptions that explain the cause of the
        /// failure.
        /// </returns>
        public abstract List<Exception> Errors
        {
            get;
        }
        
        /// <summary>
        /// A string that should contain meaningful help instructions for this
        /// build task, ideally formatted to display correctly in a typical
        /// console window. A non-string empty string is not required but
        /// strongly suggested.
        /// </summary>
        /// <returns>
        /// A string that should contain meaningful help instructions for this
        /// build task.
        /// </returns>
        public abstract string Help
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
        /// <returns>
        /// A unique GUID that identifies this build task.
        /// </returns>
        public abstract Guid GUID
        {
            get;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source">
        /// 
        /// </param>
        /// <param name="targetFileName">
        /// 
        /// </param>
        /// <param name="targetDirectory">
        /// 
        /// </param>
        /// <param name="forceCreateDirectory">
        /// 
        /// </param>
        /// <returns>
        /// 
        /// </returns>
        protected bool DownloadFile(Uri source, string targetFileName, 
            string targetDirectory, bool forceCreateDirectory = true)
        {

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="archivePath">
        /// 
        /// </param>
        /// <param name="decompressedPath">
        /// 
        /// </param>
        /// <param name="forceCreateDirectory">
        /// 
        /// </param>
        /// <remarks>
        /// May throw exceptions related to file IO or path formatting
        /// exceptions. This method is not designed to automatically handle
        /// exception, as documented in this class, it is the up to the user to
        /// handle and return exceptions via the appropriate exposed properties
        /// or methods.
        /// </remarks>
        /// <returns>
        /// 
        /// </returns>
        protected bool DecompressArchive(string archivePath, 
            string decompressedPath, bool forceCreateDirectory = true)
        {
            // Enforce create directory if specified.
            if(forceCreateDirectory)
            {
                if(!Directory.Exists(decompressedPath))
                {
                    Directory.CreateDirectory(decompressedPath);
                }
            }

            // Can't extract nothingness.
            if(!File.Exists(archivePath))
            {
                return false;
            }

            var archive = ArchiveFactory.Open(archivePath);
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory)
                {
                    Console.WriteLine(entry.Key);
                    entry.WriteToDirectory(decompressedPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                }
            }

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="workingDirectory">
        /// 
        /// </param>
        /// <param name="processName">
        /// 
        /// </param>
        /// <param name="processArgs">
        /// 
        /// </param>
        /// <param name="proccessPath">
        /// 
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
        /// <returns>
        /// 
        /// </returns>
        protected int RunProcess(string workingDirectory, string processName,
            List<string> processArgs, int waitMillisections = Timeout.Infinite, 
            string proccessPath = null, bool redirectStderr = false, bool redirectStdout = false)
        {
            // The param being  non-null is insufficient to deem it valid.
            bool procPathExists = false;

            // Cleanup process path if exists
            if(!string.IsNullOrEmpty(proccessPath) && !string.IsNullOrWhiteSpace(proccessPath))
            {
                if(proccessPath.Length > 0)
                {
                    procPathExists = true;

                    // Ensure path separator is proper for the environment.
                    proccessPath = proccessPath.Replace('/', Path.PathSeparator);
                    proccessPath = proccessPath.Replace('\\', Path.PathSeparator);

                    // Strip out the last character if it's a path separator. This is handled later.
                    if(proccessPath.Last() == Path.PathSeparator)
                    {
                        proccessPath = proccessPath.Substring(0, proccessPath.Length-1);
                    }
                }                
            }

            Process p = new Process();
            p.StartInfo.Arguments = string.Join(" ", processArgs);
            p.StartInfo.WorkingDirectory = workingDirectory;
            p.StartInfo.FileName = procPathExists == false ? processName : proccessPath + Path.PathSeparator + processName;
            
            p.ErrorDataReceived += ((sender, e) =>
            {
                if(redirectStderr)
                {
                    Console.WriteLine(e.Data);
                }
            });

            p.OutputDataReceived += ((sender, e) =>
            {
                if(redirectStdout)
                {
                    Console.WriteLine(e.Data);
                }
            });

            p.Start();

            if(waitMillisections != Timeout.Infinite)
            {
                p.WaitForExit(waitMillisections);
            }
            else
            {
                p.WaitForExit();
            }
            
            return p.ExitCode;
        }
    }
}