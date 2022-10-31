using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IntelChan.Bot.Groupme
{

    public class GroupmeBot : IChatBot
    {
        string accessToken = string.Empty;
        string botId = string.Empty;

        public event EventHandler<PathCommandArgs> HandlePathCommand;

        IConfiguration Config { get; }
        public GroupmeBot(IConfiguration config)
        {
            Config = config;

            botId = Config["groupme-bot-id"];

            if (string.IsNullOrEmpty(botId))
                throw new ApplicationException("missing config value for groupme-bot-id");
        }

        public bool IsConnected
        {
            get;
            private set;
        }

        public Task ConnectAsync(CancellationToken token)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public async Task Post(string message)
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
            if(output.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                IsConnected = false;
            }
        }

        public async Task<HttpResponseMessage> Post(string message, string remoteImageUrl)
        {
            using HttpClient client = new();
            HttpResponseMessage response = await Utilities.GetHttp(client, remoteImageUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return response;
            }
            byte[] imageData = await response.Content.ReadAsByteArrayAsync();
            var content = new ByteArrayContent(imageData);
            content.Headers.Add("X-Access-Token", new string[] { accessToken });
            if (remoteImageUrl.EndsWith("jpg")) content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            else if (remoteImageUrl.EndsWith("png")) content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

            response = await client.PostAsync("https://image.groupme.com/pictures", content);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
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

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}