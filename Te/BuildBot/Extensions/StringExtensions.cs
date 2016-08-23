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

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BuildBot
{
    namespace Extensions
    {
        /// <summary>
        /// String extensions. Mostly used for rather verbose, common string tasks.
        /// </summary>
        public static class StringExtensions
        {
            /// <summary>
            /// Returns a copy of this string where any forward/backslashes in
            /// the string have been replaced with whatever is the propery path
            /// separator for the host OS environment.
            ///
            /// This method also removes any trailing slashes at the end of the
            /// string, as for our purposes, we want to enforce that directory
            /// paths never have trailing path separators.
            /// </summary>
            /// <param name="value">
            /// This string.
            /// </param>
            /// <returns>
            /// A copy of this string where any forward/backslashes in the
            /// string have been replaced with whatever is the propery path
            /// separator for the host OS environment, and any trailing path
            /// separators are removed.
            /// </returns>
            public static string ConvertToHostOsPath(this string value)
            {
                if (value.Length > 0)
                {
                    // Regex replace all slashes of any kind, greedy match, with path separator.
                    value = Regex.Replace(value, "[/]+", Path.PathSeparator.ToString());
                    value = Regex.Replace(value, "[\\]+", Path.PathSeparator.ToString());

                    // Strip out the last character if it's a path separator. In all cases, we 
                    // don't want trailing separator on directory path strings.
                    if(value.Last() == Path.PathSeparator)
                    {
                        value = value.Substring(0, value.Length-1);
                    }
                }
                
                return value;
            }

            /// <summary>
            /// Attempts to determine if this string contains illegal path characters.
            /// </summary>
            /// <param name="value"></param>
            /// <remarks>
            /// Note - This is incomplete, as per the docs: 
            /// https://msdn.microsoft.com/en-us/library/system.io.path.getinvalidpathchars(v=vs.110).aspx
            /// 
            /// XXX TODO - Roll completely implementation for each supported platform.
            /// </remarks>
            /// <returns>
            /// True if the string contains illegal path characters, false otherwise.
            /// </returns>
            public static bool ContainsInvalidPathCharacters(this string value)
            {
                return value.IndexOfAny(Path.GetInvalidPathChars()) != -1;
            }

            /// <summary>
            /// Attempts to determine if this string contains illegal file name characters.
            /// </summary>
            /// <param name="value"></param>
            /// <remarks>
            /// Note - This is incomplete, as per the docs: 
            /// https://msdn.microsoft.com/en-us/library/system.io.path.getinvalidpathchars(v=vs.110).aspx
            /// 
            /// XXX TODO - Roll completely implementation for each supported platform.
            /// </remarks>
            /// <returns>
            /// True if the string contains illegal path characters, false otherwise.
            /// </returns>
            public static bool ContainsInvalidFileNameCharacters(this string value)
            {
                return value.IndexOfAny(Path.GetInvalidFileNameChars()) != -1;
            }
        }
    }
}