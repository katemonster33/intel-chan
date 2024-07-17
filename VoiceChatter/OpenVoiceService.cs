using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Collections.Specialized;
using Microsoft.Extensions.Configuration;

namespace IntelChan.VoiceChatter
{
    public class OpenVoiceService
    {
        string Endpoint { get; }

        public OpenVoiceService(IConfiguration config)
        {
            Endpoint = config["openvoice-endpoint"] ?? "http://127.0.0.1:7861/base_tts/";
        }

        public async Task<string?> RequestAudio(string prompt, string fileName)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //MultipartFormDataContent formContent = new MultipartFormDataContent();
            //formContent.Add(new StringContent(prompt), "text");
            //formContent.Add(new StringContent("en-newest"), "accent");

            NameValueCollection httpQuery = HttpUtility.ParseQueryString(string.Empty);
            httpQuery["text"] = prompt;
            httpQuery["accent"] = "en-newest";
            httpQuery["speed"] = "1";
            // Request MPEG
            var response = await client.GetAsync($"{Endpoint}?text={prompt}&accent=en-newest&speed=1");

            // Output Response to local MPEG file in the respective directory
            if (response != null)
            {

                int fileNameExtension = 0;
                int retries = 0;
                bool fileNameValid = false;

                while (!fileNameValid)
                {
                    try
                    {
                        if (!Directory.Exists("Audio"))
                        {
                            Directory.CreateDirectory("Audio");
                        }
                        // Stream response as binary data into a file
                        using (Stream stream = await response.Content.ReadAsStreamAsync())
                        using (FileStream fileStream = File.Create(@"./Audio/" + fileName + fileNameExtension.ToString() + ".wav"))
                        {
                            await stream.CopyToAsync(fileStream);
                        }

                        fileNameValid = true;

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception in MetaVoice/RequestAudio(): " + ex.Message);
                        retries++;
                        if (retries >= 50)
                            break;
                        fileNameExtension++;
                    }
                }

                if (fileNameValid)
                    return $"./Audio/{fileName}{fileNameExtension}.wav";
            }
            return null;
        }
    }
}
