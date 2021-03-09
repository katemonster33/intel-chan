using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace IntelChan
{
    public sealed class Utilities
    {
        public static async Task<T> ReadResponseAsJson<T>(HttpResponseMessage response)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync());
            }
            catch(JsonException)
            {
                Console.WriteLine("Invalid JSON.");
                return default(T);
            } 
        }

        public static void PopulateUserAgent(HttpRequestHeaders headers)
        {
            // Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.72 Safari/537.36
            headers.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
            headers.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
            headers.UserAgent.Add(new ProductInfoHeaderValue("AppleWebKit", "537.36"));
            headers.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
            headers.UserAgent.Add(new ProductInfoHeaderValue("Chrome", "89.0.4389.72"));
            headers.UserAgent.Add(new ProductInfoHeaderValue("Safari", "537.36"));
        }

        public static Task<HttpResponseMessage> GetHttp(HttpClient client, string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            PopulateUserAgent(request.Headers);
            return client.SendAsync(request);
        }

        public static Task<HttpResponseMessage> PostHttp(HttpClient client, string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            PopulateUserAgent(request.Headers);
            return client.SendAsync(request);
        }

        public static Task<HttpResponseMessage> PostJson<T>(HttpClient client, string requestUri, T jsonObject, JsonSerializerOptions options = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = JsonContent.Create(jsonObject);
            PopulateUserAgent(request.Headers);
            return client.SendAsync(request);
        }
    }
}