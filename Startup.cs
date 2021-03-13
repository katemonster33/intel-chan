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
                .Build();

            
            var myServices = new ServiceCollection();

            myServices.AddSingleton<IConfiguration>(config);
            myServices.AddSingleton<GroupmeBot>();
            myServices.AddSingleton<LocalTripwire>();
            myServices.AddSingleton<ZkillClient>();
            return myServices.BuildServiceProvider();
        }
    }
}