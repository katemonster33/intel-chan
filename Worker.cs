using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Groupme;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tripwire;
using Zkill;

namespace IntelChan
{

    public class Worker : IHostedService
    {
        IConfigurationRoot Configuration { get; set; }
        IServiceProvider Services { get; set; }
        ILogger<Worker> Logger { get; set; }

        IZkillClient ZkillClient { get; }
        TripwireLogic TripwireLogic { get; }
        IGroupmeBot GroupmeBot { get; }


        public Worker(IZkillClient zkillClient, IGroupmeBot groupmeBot, TripwireLogic tripwire,ILogger<Worker> logger)
        {
            ZkillClient = zkillClient;
            GroupmeBot = groupmeBot;
            TripwireLogic = tripwire;
            Logger=logger;
        }
        public async Task StartAsync(CancellationToken token)
        {

            await ZkillClient.ConnectAsync(token);

            if (!ZkillClient.Connected)
            {
                Console.WriteLine("Could not connect to Zkill.");
                Environment.Exit(-1);
            }
            ZkillClient.KillReceived += async (sender, link) =>
            {
                await GroupmeBot?.Post(link);
            };
            Console.WriteLine("Tripwire / Zkill connection successful, kill report subscriptions should commence shortly.");
            Logger.LogInformation("Ready");
            List<string> subscribedSystemIds = new List<string>();
            do
            {
                if (!ZkillClient.Connected)
                {
                    await ZkillClient.ConnectAsync(token);
                }


                var response = await TripwireLogic.GetChains();
                DateTime syncTime = TripwireLogic.SyncTime;
                if (response.Count > 0)
                {
                    List<string> systemIds = new List<string>();
                    foreach (var chain in response)
                    {
                        TripwireLogic.GetChainSystemIds(chain, ref systemIds);
                    }
                    systemIds = systemIds.Distinct().ToList();

                    List<string> addedSigs = systemIds.Except(subscribedSystemIds).ToList();
                    await ZkillClient.SubscribeSystems(addedSigs);

                    List<string> removedSigs = subscribedSystemIds.Except(systemIds).ToList();
                    await ZkillClient.UnsubscribeSystems(removedSigs);
                    if (addedSigs.Any() || removedSigs.Any())
                    {
                        subscribedSystemIds = new List<string>(systemIds);
                        Console.WriteLine($"Subscribed to {addedSigs.Count} systems, unsubscribed from {removedSigs.Count} systems.");
                        Logger.LogInformation($"Subscribed to {addedSigs.Count} systems, unsubscribed from {removedSigs.Count} systems.");
                    }
                }

                if(token.IsCancellationRequested)
                    break;
                Thread.Sleep(1000);
            }
            while (true);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping");
            return Task.CompletedTask;
        }
    }
}