using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.Primitives;
using NAudio.MediaFoundation;

namespace IntelChan.Bot.Discord
{
    public class StableDiffusionCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("draw", "Draw an image using Stable Diffusion", runMode: RunMode.Async)]
        public async Task Draw(string positivePrompt, string negativePrompt = "nsfw, nudity", int width = 512, int height = 512, string sampler = "Euler a", IAttachment? image = null)
        {
            await DeferAsync();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://127.0.0.1:7860");
            HttpResponseMessage resp;
            byte[]? data = null;
            if (image != null)
            {
                data = await Utilities.Download(image.Url);
            }
            if(data != null)
            {
                File.WriteAllBytes("tmp.png", data);
                resp = await Utilities.PostJson(client, "/sdapi/v1/img2img", 
                new StableDiffusion.Img2ImgRequest() 
                { 
                    prompt = positivePrompt, 
                    init_images = new List<string>() { Convert.ToBase64String(data) }, 
                    negative_prompt = negativePrompt, 
                    sampler_index = sampler 
                });
            }
            else
            {
                resp = await Utilities.PostJson(client, "/sdapi/v1/txt2img", 
                    new StableDiffusion.Txt2ImgRequest() 
                    { 
                        prompt = positivePrompt, 
                        negative_prompt = negativePrompt, 
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
                    await FollowupWithFileAsync("sd.png");
                }
            }
            else await FollowupAsync("Did not receive a valid response from Stable Diffusion. Is the service down?");
        }
    }
}