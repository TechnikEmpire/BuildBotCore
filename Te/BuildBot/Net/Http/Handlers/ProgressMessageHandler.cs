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

using System.Net.Http;

namespace BuildBot
{
    namespace Net
    {
        namespace Http
        {
            namespace Handlers
            {

                /// <summary>
                /// This delegate is used to track the progress of an HTTP
                /// request or response.
                /// </summary>                    
                /// <param name="bytesTransmitted">
                /// The total number of bytes written or read thus far.
                /// </param>
                /// <param name="bytesTotal">
                /// The total number of bytes to be read or written to or from the HTTP stream.
                /// This will be zero in the event that the HTTP transaction is a response and is
                /// being delivered as a chunked response.
                /// </param>
                public delegate void HttpTransactionProgressHandler(ulong bytesTransmitted, ulong bytesTotal);
            }
        }
    }
}