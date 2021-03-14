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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntelChan
{
    class Program
    {
        static IConfigurationRoot Configuration { get; set; }
        static IServiceProvider Services { get; set; }

        static async Task Main(string[] args)
        {
            Services = Startup.ConfigureServices(args);

            var tripwire = Services.GetService<TripwireLogic>();
            var groupmeBot = Services.GetService<IGroupmeBot>();
            var zkillClient = Services.GetService<IZkillClient>();

            await zkillClient.ConnectAsync();

            if (!zkillClient.Connected)
            {
                Console.WriteLine("Could not connect to Zkill.");
                Environment.Exit(-1);
            }
            zkillClient.KillReceived += async (sender, link) =>
            {
                await groupmeBot?.Post(link);
            };
            Console.WriteLine("Tripwire / Zkill connection successful, kill report subscriptions should commence shortly.");
            List<string> subscribedSystemIds = new List<string>();
            do
            {
                if (!zkillClient.Connected)
                {
                    await zkillClient.ConnectAsync();
                }


                var response = await tripwire.GetChains();
                DateTime syncTime = tripwire.SyncTime;
                if (response.Count > 0)
                {
                    List<string> systemIds = new List<string>();
                    foreach (var chain in response)
                    {
                        tripwire.GetChainSystemIds(chain, ref systemIds);
                    }
                    systemIds = systemIds.Distinct().ToList();

                    List<string> addedSigs = systemIds.Except(subscribedSystemIds).ToList();
                    await zkillClient.SubscribeSystems(addedSigs);

                    List<string> removedSigs = subscribedSystemIds.Except(systemIds).ToList();
                    await zkillClient.UnsubscribeSystems(removedSigs);
                    if (addedSigs.Any() || removedSigs.Any())
                    {
                        subscribedSystemIds = new List<string>(systemIds);
                        Console.WriteLine($"Subscribed to {addedSigs.Count} systems, unsubscribed from {removedSigs.Count} systems.");
                    }
                }


            }
            while (true);
        }


    }
}
