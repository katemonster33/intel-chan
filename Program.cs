using System.Threading.Tasks;
using Groupme;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tripwire;
using Zkill;

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
                myServices.AddSingleton<IGroupmeBot,DummyGroupmeBot>();
                myServices.AddSingleton<ITripwireDataProvider,LocalTripwireData>();
                myServices.AddSingleton<IZkillClient,ZkillClient>();
                myServices.AddSingleton<TripwireLogic>();
                
            });
    }
}
