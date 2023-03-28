using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IntelChan.Bot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tripwire;
using Zkill;
using EveSde;

namespace IntelChan
{

    public class Worker : IHostedService
    {
        IConfigurationRoot Configuration { get; set; }
        IServiceProvider Services { get; set; }
        ILogger<Worker> Logger { get; set; }

        IZkillClient ZkillClient { get; }
        TripwireLogic TripwireLogic { get; }
        IChatBot ChatBot { get; }

        IEveSdeClient SdeClient { get; }


        public Worker(IZkillClient zkillClient, IChatBot chatBot, TripwireLogic tripwire, ILogger<Worker> logger, IServiceProvider services, IEveSdeClient sdeClient)
        {
            Services = services;
            ZkillClient = zkillClient;
            ChatBot = chatBot;
            TripwireLogic = tripwire;
            Logger = logger;
            SdeClient = sdeClient;
        }

        public async Task StartAsync(CancellationToken token)
        {
            if(!SdeClient.Start())
            {
                Logger.LogError("Could not load SDE contents");
                return;
            }
            await ChatBot.ConnectAsync(token);

            int startWaitTime = Environment.TickCount;
            while (!ChatBot.IsConnected && (Environment.TickCount - startWaitTime) < 5000)
            {
                Thread.Sleep(10);
            }
            if (!ChatBot.IsConnected)
            {
                Logger.LogWarning("Could not connect to discord.");
                return;
            }
            ChatBot.HandlePathCommand += ChatBot_HandlePathCommand;

            //await ChatBot.Post("Reactor online. Sensors online. Weapons online. All systems nominal.");
            //await ChatBot.Post("Am I alive?");
            //await ChatBot.Post("Simp for me, meatbags.");
            await ZkillClient.ConnectAsync(token);
            if (!ZkillClient.Connected)
            {
                Logger.LogWarning("Could not connect to Zkill.");

                return;
            }
            ZkillClient.KillReceived += async (sender, link) =>
            {
                await ChatBot?.Post(link);
            };

            await TripwireLogic.StartAsync(token);

            if (!TripwireLogic.Connected)
            {
                Logger.LogWarning("Could not connect to Tripwire.");
                return;
            }

            Logger.LogInformation("Tripwire / Zkill connection successful, kill report subscriptions should commence shortly.");
            List<string> subscribedSystemIds = new List<string>();
            List<WormholeSystem> currentSystems = null;
            do
            {
                if (!ZkillClient.Connected)
                    await ZkillClient.ConnectAsync(token);

                var response = await TripwireLogic.GetChains(token);

                DateTime syncTime = TripwireLogic.SyncTime;
                if (response.Count > 0)
                {
                    var systems = new List<WormholeSystem>();
                    if (currentSystems == null)
                        currentSystems = new List<WormholeSystem>(systems);
                    foreach (var chain in response)
                    {
                        TripwireLogic.FlattenList(chain, ref systems);
                    }
                    var systemIds = systems.Select(x => x.SystemId).Distinct();

                    List<string> addedSigs = systemIds.Except(subscribedSystemIds).ToList();
                    await ZkillClient.SubscribeSystems(addedSigs);

                    List<string> removedSigs = subscribedSystemIds.Except(systemIds).ToList();
                    await ZkillClient.UnsubscribeSystems(removedSigs);
                    if (addedSigs.Any() || removedSigs.Any())
                    {
                        subscribedSystemIds = new List<string>(systemIds);
                        var subbed = new StringBuilder();
                        var unsubbed = new StringBuilder();

                        foreach (var sys in addedSigs)
                        {
                            var system = systems.FirstOrDefault(x => x.SystemId == sys);
                            subbed.AppendLine($"{system.SystemName}");
                        }
                        foreach (var sys in removedSigs)
                        {
                            var system = currentSystems.FirstOrDefault(x => x.SystemId == sys);
                            unsubbed.AppendLine($"{system.SystemName}");
                        }
                        //update currentsystems
                        currentSystems = systems;
                        if (addedSigs.Any())
                            Logger.LogInformation($"Subscribed to:{Environment.NewLine}{subbed.ToString()}");
                        if (removedSigs.Any())
                            Logger.LogInformation($"UnSubscribed from:{Environment.NewLine}{unsubbed.ToString()}");
                        Logger.LogInformation($"Subscribed to {addedSigs.Count} systems, unsubscribed from {removedSigs.Count} systems. Monitoring {subscribedSystemIds.Count()} systems.");
                    }
                }

                if (token.IsCancellationRequested)
                    break;
                Thread.Sleep(1000);
            }
            while (true);

            await ChatBot.DisconnectAsync();
            ChatBot.Dispose();
        }

        private async Task<string> ChatBot_HandlePathCommand(string user)
        {
            string output = string.Empty;
            if(TripwireLogic.Connected)
            {
                var response = await TripwireLogic.FindCharacter(user);
                if(response == null)
                {
                    return "Character \"" + user + "\" not found in chain :(";
                }
                else
                {
                    output = SdeClient.GetName(uint.Parse(response.SystemId));
                    var responseCopy = response;
                    var parent = response.Parent;
                    while(responseCopy.Parent != null)
                    {
                        string sig = "???";
                        if(responseCopy.ParentSignatureId != null && responseCopy.ParentSignatureId.Length > 3)
                        {
                            sig = responseCopy.ParentSignatureId.Substring(0, 3).ToUpper();
                        }
                        output = SdeClient.GetName(uint.Parse(responseCopy.Parent.SystemId)) + " -> " + sig + "|" + output;
                        responseCopy = responseCopy.Parent;
                    }
                    output = "Path to " + user + ": " + output;
                }
            }
            return output;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping");
            return Task.CompletedTask;
        }
    }
}