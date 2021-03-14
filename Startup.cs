using System;
using Groupme;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tripwire;
using Zkill;

namespace IntelChan {
    public class Startup
    {

        public static IServiceProvider ConfigureServices(string[] args)
        {
            
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json",true)
                .AddUserSecrets("3055347B-1519-4D6D-BACE-727E32EB33E9")
                .AddEnvironmentVariables()
                .Build();

            
            var myServices = new ServiceCollection()
                .AddLogging();

            myServices.AddSingleton<IConfiguration>(config);
            myServices.AddSingleton<IGroupmeBot,GroupmeBot>();
            myServices.AddSingleton<ITripwireDataProvider,LocalTripwireData>();
            myServices.AddSingleton<IZkillClient,ZkillClient>();
            myServices.AddSingleton<TripwireLogic>();
            return myServices.BuildServiceProvider();
        }
    }
}