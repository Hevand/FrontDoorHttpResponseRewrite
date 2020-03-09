using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Host;
using System.Threading;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Runtime.CompilerServices;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Net.Http.Headers;

namespace AFD.Samples.HttpResponseRewrite.Functions
{
    public class SanitizeResponseBodyFunction
    {
        private static string FUNCTION_BASE_URI = Environment.GetEnvironmentVariable("FUNCTION_BASE_URI", EnvironmentVariableTarget.Process);
        private static string TARGET_URI = Environment.GetEnvironmentVariable("TARGET_URI", EnvironmentVariableTarget.Process);

        private static HttpClientHandler _httpHandler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
        private static HttpClient _httpClient = new HttpClient(_httpHandler);

        [FunctionName("Sanitize")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Sanitize/{**path}")] HttpRequest receivedRequest,
            ILogger log, string path)
        {

            HttpRequestMessage forwardedRequest = new HttpRequestMessage(
                new HttpMethod(receivedRequest.Method), 
                $"http://{TARGET_URI}/{path}{receivedRequest.QueryString}"
            );

            //clone or manipu
            receivedRequest.CopyTo(forwardedRequest);

            HttpResponseMessage receivedResponse = await _httpClient.SendAsync(forwardedRequest);

            await SanitizeHttpResponse(receivedResponse, 
                $"{FUNCTION_BASE_URI}", 
                receivedResponse.Content.Headers.ContentType);

            return receivedResponse;
        }

        private static async Task SanitizeHttpResponse(HttpResponseMessage result, string targetUrl, System.Net.Http.Headers.MediaTypeHeaderValue contentType)
        {
            // exclude binary / other non-readable formats, like images. 
            if (contentType.MediaType.StartsWith("text", StringComparison.OrdinalIgnoreCase) // text/html, text/plain, text/javascript, text/css
             || contentType.MediaType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) // application/json.
            {
                string content = await result.Content.ReadAsStringAsync();

                result.Content = new StringContent(content.Replace(TARGET_URI, targetUrl), System.Text.Encoding.UTF8, contentType.MediaType);
            }
        }
    }
}
