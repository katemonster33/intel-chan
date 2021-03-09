using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.WebSockets;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using Tripwire;
using Zkill;
using Groupme;

namespace IntelChan
{
    class Program
    {

        static async Task<List<string>> TranslateSystemIDsToNames(List<string> systemIds)
        {
            using HttpClient client = new()
            {
                BaseAddress = new Uri("https://esi.evetech.net/v4/")
            };
            List<string> systemNames = new List<string>();
            foreach(var systemId in systemIds)
            {
                var response = await client.GetAsync($"universe/systems/{systemId}");
                if(response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JsonDocument jsonSystemInfo = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
                    double secStatus = jsonSystemInfo.RootElement.GetProperty("security_status").GetDouble();
                    //if(secStatus < 0.5)
                    //{
                        systemNames.Add(jsonSystemInfo.RootElement.GetProperty("name").GetString());
                    //}
                }
            }
            return systemNames;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: IntelChan.exe [tripwire-session-ID] [groupme-access-token] [groupme-bot-id]");
        }

        static async Task Main(string[] args)
        {
            if(args.Length < 3)
            {
                PrintUsage();
                return;
            }
            string sessionId = args[0];
            string accessToken = args[1];
            string botId = args[2];
            Tripwire.Tripwire tripwire = new Tripwire.Tripwire(sessionId);
            GroupmeBot groupmeBot = new GroupmeBot(accessToken, botId);
            using ZkillClient zkillClient = new ZkillClient();
            await zkillClient.ConnectAsync();
            bool isTripwireConnected = await tripwire.Initialize();
            if(!isTripwireConnected)
            {
                Console.WriteLine("Could not login to Tripwire. Verify you are logged in with a browser and the session ID is valid.");
                Environment.Exit(-1);
            }
            if(!zkillClient.Connected)
            {
                Console.WriteLine("Could not connect to Zkill.");
                Environment.Exit(-1);
            }
            zkillClient.KillReceived += async (sender, link) => 
            {   
                if(groupmeBot != null)
                {
                    await groupmeBot.Post(link);
                }
            };
            Console.WriteLine("Tripwire / Zkill connection successful, kill report subscriptions should commence shortly. Press any key to stop.");
            List<string> subscribedSystemIds = new List<string>(); 
            Memory<char> buffer = new Memory<char>(new char[1]);
            var readTask = Console.In.ReadAsync(buffer, new CancellationToken());
            do
            {
                if(!zkillClient.Connected)
                {
                    await zkillClient.ConnectAsync();
                }
                try
                {
                    var response = tripwire.GetChains(out DateTime syncTime);
                    if(response.Count > 0)
                    {
                        List<string> systemIds = new List<string>();
                        foreach(var chain in response)
                        {
                            tripwire.GetChainSystemIds(chain, ref systemIds);
                        }
                        systemIds = systemIds.Distinct().ToList();

                        List<string> addedSigs = systemIds.Except(subscribedSystemIds).ToList();
                        await zkillClient.SubscribeSystems(addedSigs);

                        List<string> removedSigs = subscribedSystemIds.Except(systemIds).ToList();
                        await zkillClient.UnsubscribeSystems(removedSigs);
                        if(addedSigs.Any() || removedSigs.Any())
                        {
                            subscribedSystemIds = new List<string>(systemIds);
                            Console.WriteLine($"Subscribed to {addedSigs.Count} systems, unsubscribed from {removedSigs.Count} systems.");
                        }
                    }
                }
                catch(AggregateException ae)
                {
                    ae.Handle(ex => 
                    {
                        if(ex is WebSocketException wsex)
                        {
                            return true;
                        }
                        if(ex is HttpRequestException httpex)
                        {
                            return true;
                        }
                        return false;
                    });
                }
            }
            while(!readTask.AsTask().Wait(10000));

            await zkillClient.DisconnectAsync();
        }
    }
}
