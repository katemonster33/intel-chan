using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace IntelChan

{
    public class EveClient
    {
        HttpClient MyHttpClient{get;}
        IConfiguration Config{get;}
        public EveClient(IConfiguration configuration, HttpClient client)
        {
            Config=configuration;
            MyHttpClient=client;

        }

        public async Task<List<string>> TranslateSystemIDsToNames(List<string> systemIds)
        {
            var baseurl = "https://esi.evetech.net/v4/";
            List<string> systemNames = new List<string>();
            foreach(var systemId in systemIds)
            {
                var response = await MyHttpClient.GetAsync($"{baseurl}universe/systems/{systemId}");
                if(response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var jsonSystemInfo = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
                    double secStatus = jsonSystemInfo.RootElement.GetProperty("security_status").GetDouble();
                    //if(secStatus < 0.5)
                    //{
                        systemNames.Add(jsonSystemInfo.RootElement.GetProperty("name").GetString());
                    //}
                }
            }
            return systemNames;
        }   
    }
}