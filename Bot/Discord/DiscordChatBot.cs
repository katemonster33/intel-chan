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

        public event EventHandler<PathCommandArgs> HandlePathCommand;

        IConfiguration Config { get; }

        ILogger<DiscordChatBot> Logger { get; }

        public DiscordChatBot(IConfiguration config, ILogger<DiscordChatBot> logger)
        {
            Config = config;
            Logger = logger;

            _client = new DiscordSocketClient();

            _client.Log += Log;

            _client.Ready += _client_Ready;

            _client.Disconnected += _client_Disconnected;

            _client.MessageReceived += _client_MessageReceived;

            discordBotToken = Config["discord-token"];

            if (string.IsNullOrEmpty(discordBotToken))
                throw new ApplicationException("missing config value for discord-token");
        }

        async Task ProcessChainpathCommandAsync(string messageUser, string suppliedUser)
        {

        }

        async Task _client_MessageReceived(SocketMessage arg)
        {
            if (arg == null) throw new ArgumentNullException(nameof(arg));
            if(arg.Channel != textChannel)
            {
                return;
            }
            if (arg.MentionedUsers.Contains(_client.CurrentUser))
            {

            }
            else if (arg.Content.StartsWith("!"))
            {
                // attempt to process command
                string commandName = string.Empty;
                string remainder = string.Empty;
                int firstSpaceIndex = arg.Content.IndexOf(' ');
                if (firstSpaceIndex == -1)
                {
                    commandName = arg.Content.Substring(1);
                }
                else
                {
                    commandName = arg.Content.Substring(1, firstSpaceIndex);
                    remainder = arg.Content.Substring(firstSpaceIndex + 1);
                }
                string reply = string.Empty;
                switch (commandName)
                {
                    case "chainpath":
                        {
                            var cmdArgs = new PathCommandArgs() { Character = remainder };
                            HandlePathCommand?.Invoke(this, cmdArgs);
                            reply = cmdArgs.Response;
                        }
                        break;
                }
                if(!string.IsNullOrEmpty(reply))
                {
                    await textChannel.SendMessageAsync(reply);
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

        public void OnChainPathCommand(string characterName)
        {
            HandlePathCommand?.Invoke(this, new PathCommandArgs() { Character = characterName });
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
