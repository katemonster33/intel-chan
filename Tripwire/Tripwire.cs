using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using IntelChan;
using System.Threading.Tasks;

namespace Tripwire
{
    public class Tripwire
    {
        List<string> systemIds = new List<string>();
        string phpSessionId = string.Empty;
        public Tripwire(string phpSessionId)
        {
            this.phpSessionId = phpSessionId;
        }

        public Tripwire(string phpSessionId, List<string> systemIds)
        {
            this.phpSessionId = phpSessionId;
            this.systemIds = new List<string>(systemIds);
        }

        public async Task<bool> Initialize()
        {
            using HttpClient client = new();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://dsyn-tripwire.centralus.cloudapp.azure.com/?system=");
            Utilities.PopulateUserAgent(request.Headers);
            request.Headers.Add("Cookie", $"_ga=GA1.2.728460138.1609098244; PHPSESSID={phpSessionId}; _gid=GA1.2.732828563.1615002251; _gat=1");

            var response = client.Send(request);

            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                systemIds.Clear();
                JsonDocument initJson = null;
                string responseHtml = await response.Content.ReadAsStringAsync();
                foreach(string line in responseHtml.Split('\n'))
                {
                    if(line.Contains("var init"))
                    {
                        int indexOfBracket = line.IndexOf('{');
                        if(indexOfBracket != -1)
                        {
                            initJson = JsonDocument.Parse(line.Substring(indexOfBracket, line.Length - indexOfBracket - 1));
                        }
                    }
                }
                if(initJson != null)
                {
                    var tabs = initJson.RootElement.GetProperty("options").GetProperty("chain").GetProperty("tabs").EnumerateArray();
                    foreach(var tab in tabs)
                    {
                        systemIds.Add(tab.GetProperty("systemID").GetString());
                    }
                }
            }
            return systemIds.Any();
        }

        public WormholeSystem CreateSystem(Wormhole connection, string systemId, List<Signature> signatures, List<Wormhole> wormholes, int level)
        {
            WormholeSystem output = new WormholeSystem()
            {
                SystemId = systemId,
                Children = new List<KeyValuePair<Signature, WormholeSystem>>()
            };
            List<Signature> systemSigs = signatures.Where(ss => ss.SystemID == systemId).ToList();
            foreach(var sig in systemSigs)
            {
                var hole = wormholes.FirstOrDefault(h => h.InitialID == sig.ID || h.SecondaryID == sig.ID);
                if(hole != null)
                {
                    var otherSigId = hole.InitialID == sig.ID ? hole.SecondaryID : hole.InitialID;
                    var childSig = signatures.FirstOrDefault(child => child.ID == otherSigId);
                    if(childSig != null)
                    {
                        signatures.Remove(childSig);
                        wormholes.Remove(hole);
                        output.Children.Add(new KeyValuePair<Signature, WormholeSystem>(sig, CreateSystem(hole, childSig.SystemID, signatures, wormholes, level + 1)));
                    }
                }
            }
            return output;
        }
        
        public void GetChainSystemIds(WormholeSystem chain, ref List<string> systemIds)
        {
            systemIds.Add(chain.SystemId);
            foreach(var wh in chain.Children)
            {
                if(wh.Value != null)
                {
                    GetChainSystemIds(wh.Value, ref systemIds);
                }
            }
        }

        public List<WormholeSystem> GetChains(out DateTime syncTime)
        {
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
            HttpResponseMessage response = client.Send(request);
            string responseJson = string.Empty;
            List<Signature> tripwireSigs = new List<Signature>();
            List<Wormhole> tripwireHoles = new List<Wormhole>();
            List<WormholeSystem> chains = new List<WormholeSystem>();
            syncTime = DateTime.Now;
            if(response.StatusCode == global::System.Net.HttpStatusCode.OK)
            {
                JsonDocument doc = JsonDocument.Parse(response.Content.ReadAsStream());
                syncTime = DateTime.Parse(doc.RootElement.GetProperty("sync").GetString());
                var sigs = doc.RootElement.GetProperty("signatures");
                foreach(var node in sigs.EnumerateObject())
                {
                    var sig = JsonSerializer.Deserialize<Signature>(node.Value.ToString());
                    if(sig.SystemID != null && sig.SystemID.Length > 3)
                    {
                        tripwireSigs.Add(sig);
                    }
                }
                var wormholes = doc.RootElement.GetProperty("wormholes");
                foreach(var node in wormholes.EnumerateObject())
                {
                    tripwireHoles.Add(JsonSerializer.Deserialize<Wormhole>(node.Value.ToString()));
                }
                List<string> allSystemIds = new List<string>();
                foreach(var id in systemIds)
                {
                    chains.Add(CreateSystem(null, id, tripwireSigs, tripwireHoles, 0));
                }
            }
            return chains;
        }
    }
}