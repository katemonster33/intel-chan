using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        string? discordBotToken;

        ITextChannel? textChannel;

        public event Func<string, Task<string>>? HandlePathCommand;
        public event Func<string, byte[]?, Task<string>>? HandleDrawCommand;
        public event Func<Task<List<string>>>? HandleGetModelsCommand;
        public event Func<string, Task<bool>>? HandleSetModelCommand;

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

        string mockingText(string input)
        {
            string output_text = "";

            foreach (char c in input)
            {

                if (char.IsLetter(c))
                {
                    if (Random.Shared.Next(1000) > 500)
                    {
                        output_text += char.ToUpper(c);
                    }
                    else
                    {
                        output_text += char.ToLower(c);
                    }
                }
                else
                {
                    output_text += c;
                }

            }
            return output_text;
        }
        

        async Task _client_MessageReceived(SocketMessage arg)
        {
            if (arg == null) throw new ArgumentNullException(nameof(arg));
            string cc = arg.Content;
            if(arg.Author is SocketGuildUser sgi)
            {
                if(sgi.DisplayName.Contains("Carl Bathana") || sgi.DisplayName.Contains("carl_engelke"))
                {
                    await arg.Channel.SendMessageAsync(mockingText(arg.CleanContent));
                    return;
                }
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
                        if (HandlePathCommand != null)
                        {
                            reply = await HandlePathCommand.Invoke(remainder);
                        }
                        break;
                    case "draw":
                        if (HandleDrawCommand != null)
                        {
                            byte[]? input = null;
                            if (arg.Attachments.Count > 0)
                            {
                                input = await Utilities.Download(arg.Attachments.First().Url);
                            }
                            await Task.Run(async () =>
                            {
                                string file = await HandleDrawCommand.Invoke(remainder, input);
                                if (!string.IsNullOrWhiteSpace(file))
                                {
                                    await arg.Channel.SendFileAsync(file);
                                }
                            }).ConfigureAwait(false);
                        }
                        break;
                    case "getmodels":
                        if(HandleGetModelsCommand != null)
                        {
                            var output = await HandleGetModelsCommand();
                            if(output != null)
                            {
                                await arg.Channel.SendMessageAsync(string.Join("\n", output));
                            }
                        }
                        break;

                    case "setmodel":
                        if (HandleSetModelCommand != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                var output = await HandleSetModelCommand(remainder);
                                if (output)
                                {
                                    await arg.Channel.SendMessageAsync("Set model SUCCESS!");
                                }
                                else
                                {
                                    await arg.Channel.SendMessageAsync("Set model FAILED! Check your entry!");
                                }
                            });
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
            if (textChannel != null)
            {
                await textChannel.SendMessageAsync(message);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
