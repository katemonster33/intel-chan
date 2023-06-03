using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Tripwire;

namespace IntelChan.Bot.Discord
{
    public class DiscordChatBot : IChatBot
    {
        DiscordSocketClient _client;

        string discordBotToken;

        ITextChannel textChannel;

        public event Func<string, Task<string>> HandlePathCommand;
        public event Func<string, byte[], Task<string>> HandleDrawCommand;

        IConfiguration Config { get; }

        ILogger<DiscordChatBot> Logger { get; }

        public DiscordChatBot(IConfiguration config, ILogger<DiscordChatBot> logger)
        {
            Config = config;
            Logger = logger;

            _client = new DiscordSocketClient(new DiscordSocketConfig(){GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent});

            _client.Log += Log;

            _client.Ready += _client_Ready;

            _client.Disconnected += _client_Disconnected;

            _client.MessageReceived += _client_MessageReceived;

            discordBotToken = Config["discord-token"];

            if (string.IsNullOrEmpty(discordBotToken))
                throw new ApplicationException("missing config value for discord-token");
        }

        async Task _client_MessageReceived(SocketMessage arg)
        {
            if (arg == null) throw new ArgumentNullException(nameof(arg));
            string cc = arg.CleanContent;
            while(cc.StartsWith("@") && cc.IndexOf(' ') != -1)
            {
                cc = cc.Trim().Substring(cc.IndexOf(' ') + 1);
            }
            if (cc.StartsWith("!"))
            {
                // attempt to process command
                string commandName = string.Empty;
                string remainder = string.Empty;
                int firstSpaceIndex = cc.IndexOf(' ');
                if (firstSpaceIndex == -1)
                {
                    commandName = cc.Substring(1);
                }
                else
                {
                    commandName = cc.Substring(1, firstSpaceIndex - 1);
                    remainder = cc.Substring(firstSpaceIndex + 1);
                }
                if(string.IsNullOrWhiteSpace(remainder) && arg.Author is SocketGuildUser guildUser)
                {
                    remainder = guildUser.DisplayName;
                    if(remainder.StartsWith("[-DSYN] "))
                    {
                        remainder = remainder.Substring(8);
                    }
                }
                string reply = string.Empty;
                switch (commandName)
                {
                    case "path":
                        reply = await HandlePathCommand?.Invoke(remainder);
                        break;
                    case "draw":
                        byte[] input = null;
                        if(arg.Attachments.Count > 0)
                        {
                            input = await Utilities.Download(arg.Attachments.First().Url);
                        }
                        string file = await HandleDrawCommand?.Invoke(remainder, input);
                        if(!string.IsNullOrWhiteSpace(file))
                        {
                            await arg.Channel.SendFileAsync(file);
                        }
                        break;
                }
                if(!string.IsNullOrEmpty(reply))
                {
                    await arg.Channel.SendMessageAsync(reply);
                }
            }
        }

        private Task _client_Ready()
        {
            textChannel = (ITextChannel)_client.GetChannel(1034557067614224454);

            IsConnected = true;

            return Task.CompletedTask;
        }

        private Task _client_Disconnected(Exception arg)
        {
            IsConnected = false;
            textChannel = null;
            return Task.CompletedTask;
        }

        async Task Log(LogMessage arg)
        {
            await Task.Run(() => Logger.LogInformation(arg.ToString()));
        }

        public bool IsConnected { get; private set; }

        public async Task ConnectAsync(CancellationToken token)
        {
            await _client.LoginAsync(TokenType.Bot, discordBotToken);
            await _client.StartAsync();
        }

        public async Task DisconnectAsync()
        {
            await _client.LogoutAsync();
            IsConnected = false;
            textChannel = null;
        }

        public async Task Post(string message)
        {
            await textChannel?.SendMessageAsync(message);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
