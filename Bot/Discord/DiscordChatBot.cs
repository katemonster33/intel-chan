using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
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

            discordBotToken = Config["discord-token"];

            if (string.IsNullOrEmpty(discordBotToken))
                throw new ApplicationException("missing config value for discord-token");
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
