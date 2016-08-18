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