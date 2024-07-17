using System.Threading.Tasks;
using IntelChan.Bot;
using IntelChan.Bot.Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zkill;
using IntelChan.OpenAI;
using IntelChan.VoiceChatter;

namespace IntelChan
{
    class Program
    {
        static Task Main(string[] args)=>
             CreateHostBuilder(args).Build()
                .RunAsync();
        

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseEnvironment("Development") // without this it won't load secrets
            .ConfigureServices((hostContext,myServices) =>
            {
                myServices.AddHostedService<Worker>();
                myServices.AddSingleton<IChatBot, DiscordChatBot>();
                myServices.AddSingleton<IZkillClient,ZkillClient>();
                myServices.AddSingleton<OpenAIService>();
                myServices.AddSingleton<OpenVoiceService>();
            });
    }
}
