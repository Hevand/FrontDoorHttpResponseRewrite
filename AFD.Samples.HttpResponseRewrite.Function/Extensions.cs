using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Net.Http;

namespace AFD.Samples.HttpResponseRewrite.Functions
{
    public static class Extensions
    {
        public static void CopyTo(this HttpRequest source, HttpRequestMessage destination)
        {
            foreach (string headerKey in source.Headers.Keys)
            {
                switch (headerKey)
                {
                    // Handled by platform, ignored
                    case "Connection":
                    case "Content-Length":
                    case "Date":
                    case "Expect":
                    case "Host":
                    case "If-Modified-Since":
                    case "Range":
                    case "Transfer-Encoding":
                    case "Proxy-Connection":
                        break;

                    // Special cases. Handle separately.
                    case "Accept":
                    case "AcceptEncoding":
                    case "User-Agent":
                        break;

                    // Copy
                    default:
                        destination.Headers.Add(headerKey, source.Headers[headerKey].ToString());
                        break;
                }
            }

            destination.Headers.Accept.ParseAdd(source.Headers["Accept"].ToString());
            
            if (source.Method != "GET"
                && source.Method != "HEAD"
                && source.ContentLength > 0)
            {
                destination.Content = new StreamContent(source.Body);
            }
        }
    }
}
