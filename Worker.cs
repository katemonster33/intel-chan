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
using Microsoft.CognitiveServices.Speech;
using Zkill;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Buffers.Text;
using System.Text.Json;
using IntelChan.VoiceChatter;

namespace IntelChan
{

    public class Worker : IHostedService
    {
        IServiceProvider Services { get; set; }
        ILogger<Worker> Logger { get; set; }

        IZkillClient ZkillClient { get; }
        IChatBot ChatBot { get; }

        const string jitaSystemId = "30000142";
        const string amarrSystemId = "30002187";


        public Worker(IZkillClient zkillClient, IChatBot chatBot, ILogger<Worker> logger, IServiceProvider services)
        {
            Services = services;
            ZkillClient = zkillClient;
            ChatBot = chatBot;
            Logger = logger;
        }

        public async Task StartAsync(CancellationToken token)
        {
            string[] ignoredSystemIds = [jitaSystemId, amarrSystemId];
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
            ChatBot.HandleDrawCommand += ChatBot_HandleDrawCommand;
            ChatBot.HandleGetModelsCommand += ChatBot_HandleGetModelsCommand;
            ChatBot.HandleSetModelCommand += ChatBot_HandleSetModelCommand;

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
                if (ChatBot != null)
                {
                    await ChatBot.Post(link);
                }
            };
            await ZkillClient.SubscribeCorps(new List<string>(){"98277602", "98725923" }); // Volantean Nation [VOLNA] added

            Logger.LogInformation("Zkill connection successful, kill report subscriptions should commence shortly.");
            List<string> subscribedSystemIds = new List<string>();
            do
            {
                if (!ZkillClient.Connected)
                {
                    await ZkillClient.ConnectAsync(token);

                    await ZkillClient.SubscribeCorps(new List<string>() { "98277602", "98725923" }); // Volantean Nation [VOLNA] added
                }

                if (token.IsCancellationRequested)
                    break;
                Thread.Sleep(1000);
            }
            while (true);

            try
            {
                await ZkillClient.DisconnectAsync();
                ZkillClient.Dispose();
            }
            catch(Exception)
            {
                Logger.LogWarning("Couldn't kill zkill");
            }
            await ChatBot.DisconnectAsync();
            ChatBot.Dispose();
        }

        private async Task<bool> ChatBot_HandleSetModelCommand(string arg)
        {
            var models = await GetStableDiffusionModels();
            string? modelTitle = null;
            if (models != null)
            {
                foreach(var mod in models)
                {
                    if(mod.ModelName == arg)
                    {
                        modelTitle = mod.Title; 
                        break;
                    }
                }
            }
            if(modelTitle == null)
            {
                return false;
            }
            var newOptions = new StableDiffusion.Options()
            {
                SdModelCheckpoint = modelTitle
            };
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:7860");
                List<string> output = new List<string>();
                var resp = await Utilities.PostJson(client, "/sdapi/v1/options", newOptions, Utilities.GetSerializerOptions());
                return resp.StatusCode == System.Net.HttpStatusCode.OK;
            }
        }

        async Task<List<StableDiffusion.SdModel>> GetStableDiffusionModels()
        {
            List<StableDiffusion.SdModel> output = new List<StableDiffusion.SdModel>();
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:7860");
                var resp = await Utilities.GetHttp(client, "/sdapi/v1/sd-models");
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string respJson = await resp.Content.ReadAsStringAsync();
                    var models = JsonSerializer.Deserialize<List<StableDiffusion.SdModel>>(respJson, Utilities.GetSerializerOptions());
                    if (models != null)
                    {
                        output = new List<StableDiffusion.SdModel>(models);
                    }
                }
            }
            return output;
        }

        private async Task<List<string>> ChatBot_HandleGetModelsCommand()
        {
            var models = await GetStableDiffusionModels();
            List<string> output = new List<string>();
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:7860");
                var resp = await Utilities.GetHttp(client, "/sdapi/v1/options");
                string? curModel = null;
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string respJson = await resp.Content.ReadAsStringAsync();
                    var options = JsonSerializer.Deserialize<StableDiffusion.Options>(respJson, Utilities.GetSerializerOptions());
                    if (options != null)
                    {
                        curModel = options.SdModelCheckpoint;
                    }
                }
                foreach (var mod in models)
                {
                    if (curModel != null && mod.Title == curModel)
                    {
                        output.Add("* " + mod.ModelName + " *");
                    }
                    else
                    {
                        output.Add(mod.ModelName);
                    }
                }
            }
            return output;
        }

        private async Task<string> ChatBot_HandleDrawCommand(string arg, byte[]? data)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://127.0.0.1:7860");
            string prompt = "", neg_prompt = "nsfw, nudity", sampler = "Euler a";
            int width = 512, height = 512;
            string[] tokens = arg.Split(';');
            if(tokens.Length >= 1)
            {
                prompt = tokens[0];
                if (tokens.Length >= 2) {
                    neg_prompt = tokens[1];
                    if (tokens.Length >= 3 && tokens[2].Contains('x'))
                    {
                        string[] res = tokens[2].Split('x');
                        width = int.Parse(res[0]);
                        height = int.Parse(res[1]);
                        if(tokens.Length >= 4)
                        {
                            sampler = tokens[3];
                        }
                    }
                }
            }
            HttpResponseMessage resp;
            if (data != null)
            {
                File.WriteAllBytes("tmp.png", data);
                resp = await Utilities.PostJson(client, "/sdapi/v1/img2img", 
                    new StableDiffusion.Img2ImgRequest() 
                    { 
                        prompt = prompt, 
                        init_images = new List<string>() { Convert.ToBase64String(data) }, 
                        negative_prompt = neg_prompt, 
                        sampler_index = sampler 
                    });
            }
            else
            {
                resp = await Utilities.PostJson(client, "/sdapi/v1/txt2img", 
                    new StableDiffusion.Txt2ImgRequest() 
                    { 
                        prompt = prompt, 
                        negative_prompt = neg_prompt, 
                        width = width, 
                        height = height, 
                        sampler_index = sampler 
                    }, 
                    new JsonSerializerOptions() 
                    { 
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
                    });
            }
            if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string respJson = await resp.Content.ReadAsStringAsync();
                var imgResp = JsonSerializer.Deserialize<StableDiffusion.Txt2ImgResponse>(respJson);
                if (imgResp != null)
                {
                    byte[] output = Convert.FromBase64String(imgResp.images[0]);
                    File.WriteAllBytes("sd.png", output);
                    return "sd.png";
                }
            }
            return string.Empty;
        }

        //private async Task<string> ChatBot_HandlePathCommand(string user)
        //{
        //    string output = string.Empty;
        //    if(TripwireLogic.Connected)
        //    {
        //        var response = await TripwireLogic.FindCharacter(user);
        //        if(response == null)
        //        {
        //            return "Character \"" + user + "\" not found in chain :(";
        //        }
        //        else
        //        {
        //            output = SdeClient.GetName(uint.Parse(response.SystemId));
        //            var responseCopy = response;
        //            var parent = response.Parent;
        //            while(responseCopy.Parent != null)
        //            {
        //                string sig = "???";
        //                if(responseCopy.ParentSignatureId != null && responseCopy.ParentSignatureId.Length > 3)
        //                {
        //                    sig = responseCopy.ParentSignatureId.Substring(0, 3).ToUpper();
        //                }
        //                output = SdeClient.GetName(uint.Parse(responseCopy.Parent.SystemId)) + " -> " + sig + "|" + output;
        //                responseCopy = responseCopy.Parent;
        //            }
        //            output = "Path to " + user + ": " + output;
        //        }
        //    }
        //    return output;
        //}

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping");
            return Task.CompletedTask;
        }
    }
}