using System.Threading.Tasks;
using IntelChan.Bot;
using IntelChan.Bot.Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tripwire;
using Zkill;
using EveSde;

namespace IntelChan
{
    class Program
    {
        static Task Main(string[] args)=>
             CreateHostBuilder(args).Build()
                .RunAsync();
        

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext,myServices) =>
            {
                myServices.AddHostedService<Worker>();
                myServices.AddSingleton<IChatBot, DiscordChatBot>();
                myServices.AddSingleton<ITripwireDataProvider,RemoteTripwireData>();
                myServices.AddSingleton<IZkillClient,ZkillClient>();
                myServices.AddSingleton<IEveSdeClient, EveSdeClient>();
                myServices.AddSingleton<TripwireLogic>();
                
            });
    }
}
