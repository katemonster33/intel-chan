using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IntelChan.VoiceChatter
{
    internal class MetaVoice
    {
        public static async Task<string?> RequestAudio(string prompt, string fileName)
        {
            string endpoint = "http://127.0.0.1:58004/tts";

            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            MultipartFormDataContent formContent = new MultipartFormDataContent();
            formContent.Add(new StringContent("1"), "guidance");
            formContent.Add(new StringContent("0.9"), "top_p");
            formContent.Add(new StringContent(prompt), "text");
            formContent.Add(new StringContent("assets/bria.mp3"), "speaker_ref_path");
            formContent.Add(new StringContent(""), "audiodata");

            // Request MPEG
            var response = await client.PostAsync(endpoint, formContent);

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
                        using (FileStream fileStream = File.Create(@".\Audio\" + fileName + fileNameExtension.ToString() + ".mp3"))
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
                    return @".\Audio\" + fileName + fileNameExtension.ToString() + ".mp3";
            }
            return null;
        }
    }
}
