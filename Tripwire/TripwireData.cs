using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using IntelChan;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Net.WebSockets;
using System.Threading;

namespace Tripwire
{
    public class TripwireData : ITripwireDataProvider
    {
        public bool Connected{get;set;}
        CancellationToken CancelToken{get;set;}
        List<string> systemIds = new List<string>();
        public IList<string> SystemIds { get => systemIds; }
        string phpSessionId = string.Empty;
        JsonDocument jsonDocument;
        IConfiguration Configuration { get; }
        public DateTime SyncTime { get => _syncTime; }

        DateTime _syncTime;
        public TripwireData(IConfiguration config)
        {
            Configuration = config;
        }

        async Task<bool> PopulatePHPSessionId()
        {
            using HttpClient client = new();

            var request = new HttpRequestMessage(HttpMethod.Get, "https://dsyn-tripwire.centralus.cloudapp.azure.com/");
            Utilities.PopulateUserAgent(request.Headers);

            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/webp"));

            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("br"));

            var response = await client.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var cookies = response.Headers.GetValues("Set-Cookie");
                phpSessionId = string.Empty;
                foreach (var cookie in cookies)
                {
                    if (cookie.StartsWith("PHPSESSID="))
                    {
                        phpSessionId = cookie.Substring(10);
                        if (phpSessionId.IndexOf(';') != -1)
                        {
                            phpSessionId = phpSessionId.Substring(0, phpSessionId.IndexOf(';'));
                        }
                    }
                }
            }
            return !string.IsNullOrEmpty(phpSessionId);
        }

        private async Task<bool> Login(CancellationToken token)
        {
            var username = Configuration["tripwire-username"];
            var password = Configuration["tripwire-password"];

            bool gotSessionId = await PopulatePHPSessionId();
            if (!gotSessionId) return false;

            using HttpClient client = new();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://dsyn-tripwire.centralus.cloudapp.azure.com/login.php");
            Utilities.PopulateUserAgent(request.Headers);

            request.Headers.Add("Cookie", $"_ga=GA1.2.728460138.1609098244; PHPSESSID={phpSessionId}; _gid=GA1.2.732828563.1615002251; _gat=1");

            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("br"));

            request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("mode", "login"),
                new KeyValuePair<string, string>("fakeusernameremembered", ""),
                new KeyValuePair<string, string>("fakepasswordremembered", ""),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request, token);

            }
            catch (AggregateException ae)
            {
                ae.Handle(ex =>
                {
                    if (ex is WebSocketException wsex)
                    {
                        return true;
                    }
                    if (ex is HttpRequestException httpex)
                    {
                        return true;
                    }
                    return false;
                });
            }

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }
            JsonDocument initJson = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            string responseHtml = await response.Content.ReadAsStringAsync();
            if (initJson == null || !initJson.RootElement.TryGetProperty("result", out JsonElement result) || result.GetString() != "success")
            {
                return false;
            }
            if (!systemIds.Any())
            {
                var tabs = initJson.RootElement.GetProperty("session").GetProperty("options").GetProperty("chain").GetProperty("tabs");
                foreach (var tab in tabs.EnumerateArray())
                {
                    systemIds.Add(tab.GetProperty("systemID").GetString());
                }
            }
            return true;
        }


        private async Task GetJsonData(CancellationToken token)
        {
            List<Signature> tripwireSigs = new List<Signature>();
            List<Wormhole> tripwireHoles = new List<Wormhole>();
            List<WormholeSystem> chains = new List<WormholeSystem>();

            using HttpClient client = new();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://dsyn-tripwire.centralus.cloudapp.azure.com/refresh.php");
            Utilities.PopulateUserAgent(request.Headers);
            request.Headers.Add("Cookie", $"_ga=GA1.2.728460138.1609098244; PHPSESSID={phpSessionId}; _gid=GA1.2.732828563.1615002251; _gat=1");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("signatureCount","132"),
                new KeyValuePair<string, string>("signatureTime","2021-03-06+02:52:46"),
                new KeyValuePair<string, string>("flareCount","0"),
                new KeyValuePair<string, string>("flareTime","03/06/2021+03:45:23+UTC"),
                new KeyValuePair<string, string>("commentCount","0"),
                new KeyValuePair<string, string>("commentTime", ""),
                new KeyValuePair<string, string>("mode", "refresh"),
                new KeyValuePair<string, string>("systemID", "30000142"),
                new KeyValuePair<string, string>("systemName","Jita"),
                new KeyValuePair<string, string>("instance","1615002323.5"),
                new KeyValuePair<string, string>("version","1.16")
            });
            HttpResponseMessage response = await client.SendAsync(request, token);
            string responseJson = string.Empty;

            _syncTime = DateTime.Now;

            if (response.StatusCode != global::System.Net.HttpStatusCode.OK)
                return;

            jsonDocument = JsonDocument.Parse(response.Content.ReadAsStream());
            _syncTime = DateTime.Parse(jsonDocument.RootElement.GetProperty("sync").GetString());
        }



        private void GetHoles(List<Wormhole> tripwireHoles, JsonDocument doc)
        {
            var wormholes = doc.RootElement.GetProperty("wormholes");
            foreach (var node in wormholes.EnumerateObject())
            {
                tripwireHoles.Add(JsonSerializer.Deserialize<Wormhole>(node.Value.ToString()));
            }
        }

        private void GetSigs(List<Signature> tripwireSigs, JsonDocument doc)
        {
            var sigs = doc.RootElement.GetProperty("signatures");
            foreach (var node in sigs.EnumerateObject())
            {
                var sig = JsonSerializer.Deserialize<Signature>(node.Value.ToString());
                if (sig.SystemID != null && sig.SystemID.Length > 3)
                {
                    tripwireSigs.Add(sig);
                }
            }
        }

        public async Task<IList<Wormhole>> GetHoles()
        {
            await EnsureData();
            var retList = new List<Wormhole>();
            GetHoles(retList, jsonDocument);
            return retList;
        }

        private async Task<bool> EnsureData()
        {
            if (jsonDocument == null)
            {
                Connected= await Login(CancelToken);
                if(!Connected)
                    return false;
                await GetJsonData(CancelToken);
            }
            return true;
        }

        public async Task<bool> Start(CancellationToken token)
        {
            CancelToken = token;
            return await EnsureData();
        }

        public async Task<IList<Signature>> GetSigs()
        {
            await EnsureData();
            var retLIst = new List<Signature>();
            GetSigs(retLIst, jsonDocument);
            return retLIst;
        }
    }

}