using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using IntelChan;

namespace Groupme
{
    public class GroupmeBot
    {
        string accessToken = string.Empty;
        string botId = string.Empty;

        public GroupmeBot(string accessToken, string botId)
        {
            this.accessToken = accessToken;
            this.botId = botId;
        }

        public async Task<HttpResponseMessage> Post(string message)
        {
            using HttpClient groupmeClient = new()
            {
                //BaseAddress = new Uri("https://api.groupme.com/v3/")
            };
            string json = JsonSerializer.Serialize(new Post()
            {
                 BotId = botId, 
                 Text = message
            });
            var output = await groupmeClient.PostAsJsonAsync("https://api.groupme.com/v3/bots/post", new Post()
            {
                 BotId = botId, 
                 Text = message
            });
            return output;
        }

        public async Task<HttpResponseMessage> Post(string message, string remoteImageUrl)
        {
            using HttpClient client = new();
            HttpResponseMessage response = await Utilities.GetHttp(client, remoteImageUrl);
            if(response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return response;
            }
            byte[] imageData = await response.Content.ReadAsByteArrayAsync();
            var content = new ByteArrayContent(imageData);
            content.Headers.Add("X-Access-Token", new string[] { accessToken });
            if(remoteImageUrl.EndsWith("jpg")) content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            else if(remoteImageUrl.EndsWith("png")) content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            
            response = await client.PostAsync("https://image.groupme.com/pictures", content);
            if(response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return response;
            }
            UploadResponse resp = await Utilities.ReadResponseAsJson<UploadResponse>(response);
            Post p = new Post()
            {
                BotId = botId, 
                Text = message,
                Attachments = new System.Collections.Generic.List<Attachment>()
                {
                    new Attachment(){ Type = "image", URL = resp.Payload.Url }
                }
            };
            string jsonString = JsonSerializer.Serialize(p);
            return await client.PostAsJsonAsync("https://api.groupme.com/v3/bots/post", p);
        }
    }
}