using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                Logger.LogWarning("Could not connect to Zkill.");

                return;
            }
            ZkillClient.KillReceived += async (sender, link) =>
            {
                await GroupmeBot?.Post(link);
            };

            await TripwireLogic.StartAsync(token);

            if(!TripwireLogic.Connected)
                return;

            Logger.LogInformation("Tripwire / Zkill connection successful, kill report subscriptions should commence shortly.");
            List<string> subscribedSystemIds = new List<string>();
            List<WormholeSystem> currentSystems=null;
            do
            {
                if (!ZkillClient.Connected)
                    await ZkillClient.ConnectAsync(token);

                var response = await TripwireLogic.GetChains();

                DateTime syncTime = TripwireLogic.SyncTime;
                if (response.Count > 0)
                {
                    var systems = new List<WormholeSystem>();
                    if(currentSystems == null)
                        currentSystems = new List<WormholeSystem>(systems);
                    foreach (var chain in response)
                    {
                        TripwireLogic.FlattenList(chain, ref systems);
                    }
                    var systemIds = systems.Select(x=>x.SystemId).Distinct();

                    List<string> addedSigs = systemIds.Except(subscribedSystemIds).ToList();
                    await ZkillClient.SubscribeSystems(addedSigs);

                    List<string> removedSigs = subscribedSystemIds.Except(systemIds).ToList();
                    await ZkillClient.UnsubscribeSystems(removedSigs);
                    if (addedSigs.Any() || removedSigs.Any())
                    {
                        subscribedSystemIds = new List<string>(systemIds);
                        var subbed=  new StringBuilder();
                        var unsubbed=new StringBuilder();

                        foreach(var sys in addedSigs){
                            var system = systems.FirstOrDefault(x=>x.SystemId==sys);
                            subbed.AppendLine($"{system.SystemName}");
                        }
                        foreach(var sys in removedSigs){
                            var system = currentSystems.FirstOrDefault(x=>x.SystemId==sys);
                            unsubbed.AppendLine($"{system.SystemName}");
                        }
                        //update currentsystems
                        currentSystems = systems;
                        if(addedSigs.Any())
                            Logger.LogInformation($"Subscribed to:{Environment.NewLine}{subbed.ToString()}");
                        if(removedSigs.Any())
                            Logger.LogInformation($"UnSubscribed from:{Environment.NewLine}{unsubbed.ToString()}");
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