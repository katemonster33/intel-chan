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
using System.IO.Compression;
using System.IO;

namespace Tripwire
{
    public class RemoteTripwireData : ITripwireDataProvider
    {
        public bool Connected { get; set; }

        CancellationToken CancelToken { get; set; }

        List<string> systemIds = new List<string>();

        public IList<string> SystemIds { get => systemIds; }

        List<Wormhole> cachedHoles = new List<Wormhole>();

        List<Signature> cachedSigs = new List<Signature>();

        List<OccupiedSystem> cachedOccupiedSystems = new List<OccupiedSystem>();

        string phpSessionId = string.Empty;

        JsonDocument jsonDocument;

        IConfiguration Configuration { get; }

        public DateTime SyncTime { get => _syncTime; }

        DateTime _syncTime;
        long epochTimestamp = 0;

        public RemoteTripwireData(IConfiguration config)
        {
            Configuration = config;
        }

        async Task<bool> PopulatePHPSessionId()
        {
            using HttpClient client = new();

            var request = new HttpRequestMessage(HttpMethod.Get, "https://tripwire.eve-apps.com/");
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
        
        public async Task<IList<Occupant>> GetOccupants(string systemId)
        {
            using HttpClient client = new();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://tripwire.eve-apps.com/occupants.php");
            Utilities.PopulateUserAgent(request.Headers);

            request.Headers.Add("Cookie", $"_ga=GA1.2.728460138.1609098244; PHPSESSID={phpSessionId}; _gid=GA1.2.732828563.1615002251; _gat=1");

            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("br"));

            if(epochTimestamp == 0)
            {
                epochTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("systemID", systemId),
                new KeyValuePair<string, string>("_", ""),
            });
            
            var response = await client.SendAsync(request);

            List<Occupant> occupants = new List<Occupant>();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using(var stream = response.Content.ReadAsStream())
                using(var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                using (JsonDocument initJson = JsonDocument.Parse(gzipStream))
                {
                    var occJson = initJson.RootElement.GetProperty("occupants");
                    foreach(var occ in occJson.EnumerateArray())
                    {
                        occupants.Add(JsonSerializer.Deserialize<Occupant>(occ.ToString()));
                    }
                }
            }

            epochTimestamp++;
            return occupants;
        }

        private async Task<bool> Login(CancellationToken token)
        {
            var username = Configuration["tripwire-username"];
            var password = Configuration["tripwire-password"];

            bool gotSessionId = await PopulatePHPSessionId();
            if (!gotSessionId) return false;

            using HttpClient client = new();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://tripwire.eve-apps.com/login.php");
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
            using(var stream = response.Content.ReadAsStream())
            using(var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            using (JsonDocument initJson = JsonDocument.Parse(gzipStream))
            {
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
            }

            
            return true;
        }
        
        public async Task RefreshData(CancellationToken token)
        {
            cachedSigs = new List<Signature>();
            cachedHoles = new List<Wormhole>();

            using HttpClient client = new();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://tripwire.eve-apps.com/refresh.php");
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
            using(var stream = response.Content.ReadAsStream())
            {
                string json = new StreamReader(stream).ReadToEnd();
                jsonDocument = JsonDocument.Parse(json);
                _syncTime = DateTime.Parse(jsonDocument.RootElement.GetProperty("sync").GetString());
            }
            if(jsonDocument.RootElement.TryGetProperty("wormholes", out var wormholes))
            {
                lock(cachedHoles)
                {
                    cachedHoles.Clear();
                    foreach (var node in wormholes.EnumerateObject())
                    {
                        cachedHoles.Add(JsonSerializer.Deserialize<Wormhole>(node.Value.ToString()));
                    }
                }
            }
            if(jsonDocument.RootElement.TryGetProperty("signatures", out var sigs))
            {
                lock(cachedSigs)
                {
                    cachedSigs.Clear();
                    foreach (var node in sigs.EnumerateObject())
                    {
                        var sig = JsonSerializer.Deserialize<Signature>(node.Value.ToString());
                        if (sig.SystemID != null && sig.SystemID.Length > 3)
                        {
                            cachedSigs.Add(sig);
                        }
                    }
                }
            }
            if(jsonDocument.RootElement.TryGetProperty("occupied", out var occupants))
            {
                lock(cachedOccupiedSystems)
                {
                    cachedOccupiedSystems.Clear();
                    foreach(var node in occupants.EnumerateArray())
                    {
                        var occ = JsonSerializer.Deserialize<OccupiedSystem>(node.ToString());
                        cachedOccupiedSystems.Add(occ);
                    }
                }
                
            }
        }

        public Task<IList<OccupiedSystem>> GetOccupiedSystems()
        {
            List<OccupiedSystem> occupiedSystems = new List<OccupiedSystem>();
            lock(cachedOccupiedSystems)
            {
                occupiedSystems.AddRange(cachedOccupiedSystems);
            }
            return Task.FromResult<IList<OccupiedSystem>>(occupiedSystems);
        }

        public Task<IList<Wormhole>> GetHoles()
        {
            List<Wormhole> wormholes = new List<Wormhole>();
            lock (cachedHoles)
            {
                wormholes.AddRange(cachedHoles);
            }
            return Task.FromResult<IList<Wormhole>>(wormholes);
        }

        public async Task<bool> Start(CancellationToken token)
        {
            CancelToken = token;
            Connected = await Login(CancelToken);
            return Connected;
        }

        public Task<IList<Signature>> GetSigs()
        {
            List<Signature> sigs = new List<Signature>();
            lock (cachedSigs)
            {
                sigs.AddRange(cachedSigs);
            }
            return Task.FromResult<IList<Signature>>(sigs);
        }
    }

}