using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelChan.OpenAI;
using Discord.Interactions;
using System.Reflection;

namespace IntelChan.Bot.Discord
{
    public class DiscordChatBot : IChatBot
    {
        DiscordSocketClient _client;
        InteractionService _intHandler;
        IServiceProvider ServiceProvider { get; }

        string? discordBotToken;

        ITextChannel? textChannel;

        public event Func<string, byte[]?, Task<string>>? HandleDrawCommand;
        public event Func<Task<List<string>>>? HandleGetModelsCommand;
        public event Func<string, Task<bool>>? HandleSetModelCommand;

        IConfiguration Config { get; }

        ILogger<DiscordChatBot> Logger { get; }
        OpenAIService OpenAIService { get; }
        //OpenAIAPI OpenAIAPI { get; }

        public DiscordChatBot(IConfiguration config, IServiceProvider serviceProvider, ILogger<DiscordChatBot> logger, OpenAIService openAIService)
        {
            Config = config;
            ServiceProvider = serviceProvider;
            Logger = logger;
            OpenAIService = openAIService;
            _client = new DiscordSocketClient(new DiscordSocketConfig(){GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates | GatewayIntents.Guilds});

            _client.Log += Log;

            _client.Ready += _client_Ready;

            _client.Disconnected += _client_Disconnected;

            _client.MessageReceived += _client_MessageReceived;
            //_client.get

            _intHandler = new InteractionService(_client);

            _intHandler.Log += LogAsync;

            // Process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += HandleInteraction;

            // Also process the result of the command execution.
            _intHandler.InteractionExecuted += HandleInteractionExecute;

            discordBotToken = Config["discord-token"];

            if (string.IsNullOrEmpty(discordBotToken))
                throw new ApplicationException("missing config value for discord-token");
        }

        Task LogAsync(LogMessage logMessage)
        {
            Logger.LogInformation(logMessage.Message);
            return Task.CompletedTask;
        }

        private Task HandleInteractionExecute(ICommandInfo info, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    default:
                        break;
                }

            return Task.CompletedTask;
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                // Execute the incoming command.
                var result = await _intHandler.ExecuteCommandAsync(context, ServiceProvider);

                // Due to async nature of InteractionFramework, the result here may always be success.
                // That's why we also need to handle the InteractionExecuted event.
                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            // implement
                            break;
                        default:
                            break;
                    }
            }
            catch
            {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        // string mockingText(string input)
        // {
        //     string output_text = "";

        //     foreach (char c in input)
        //     {

        //         if (char.IsLetter(c))
        //         {
        //             if (Random.Shared.Next(1000) > 500)
        //             {
        //                 output_text += char.ToUpper(c);
        //             }
        //             else
        //             {
        //                 output_text += char.ToLower(c);
        //             }
        //         }
        //         else
        //         {
        //             output_text += c;
        //         }

        //     }
        //     return output_text;
        // }

        async Task _client_MessageReceived(SocketMessage arg)
        {
            if (arg == null) throw new ArgumentNullException(nameof(arg));
            string cc = arg.Content;
            
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
                    case "draw":
                        if (HandleDrawCommand != null)
                        {
                            byte[]? input = null;
                            if (arg.Attachments.Count > 0)
                            {
                                input = await Utilities.Download(arg.Attachments.First().Url);
                            }
                            _ = Task.Run(async () =>
                            {
                                string file = await HandleDrawCommand.Invoke(remainder, input);
                                if (!string.IsNullOrWhiteSpace(file))
                                {
                                    await arg.Channel.SendFileAsync(file);
                                }
                            });
                        }
                        break;
                    case "getmodels":
                        if (HandleGetModelsCommand != null)
                        {
                            var output = await HandleGetModelsCommand();
                            if (output != null)
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

                    case "startcharacter":
                        if(OpenAIService.StartCharacter(arg.Channel.Id.ToString(), remainder))
                        {
                            await arg.Channel.SendMessageAsync("Started character successfully.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Couldn't create the character - is there a character already active in this channel?");
                        }
                        break;

                    case "stopcharacter":
                        if(OpenAIService.StopCharacter(arg.Channel.Id.ToString()))
                        {
                            await arg.Channel.SendMessageAsync("Success, character stopped.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Failed, no character session appears active in this channel.");
                        }
                        break;

                    case "savecharacter":
                        if(OpenAIService.SaveCharacter(arg.Channel.Id.ToString(), remainder))
                        {
                            await arg.Channel.SendMessageAsync($"Success, character \'{remainder}\' saved.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Couldn't save the character. Is there an active session?");
                        }
                        break;

                    case "loadcharacter":
                        if(OpenAIService.LoadCharacter(arg.Channel.Id.ToString(), remainder))
                        {
                            await arg.Channel.SendMessageAsync($"Success, character \'{remainder}\' loaded to the active session.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Couldn't load the character to the current channel. Does it exist?");
                        }
                        break;

                    case "getcharacters":
                        List<string> characters = OpenAIService.GetCharacters();

                        await arg.Channel.SendMessageAsync($"List of saved characters: [{string.Join(",", characters)}]");
                        break;


                    case "prunecharacter":
                        int numMessages = -1;
                        if(!string.IsNullOrEmpty(remainder))
                        {
                            int.TryParse(remainder, out numMessages);
                        }
                        if(OpenAIService.Prune(arg.Channel.Id.ToString(), numMessages))
                        {
                            await arg.Channel.SendMessageAsync($"Success, pruned the character log.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Couldn't prune any messages. Does the character exist? Have you spoken to it yet?");
                        }
                        break;

                    case "ask":
                        _ = Task.Run(async () =>
                        {
                            if(OpenAIService.CompleteChat(arg.Channel.Id.ToString(), remainder, out string? resp))
                            {
                                Logger.LogInformation("OpenAI said: " + reply);
                                await arg.Channel.SendMessageAsync(reply);
                            }
                            else
                            {
                                await arg.Channel.SendMessageAsync("OpenAI did not respond.");
                            }
                            //var output = await OpenAIAPI.Completions.CreateAndFormatCompletion(new OpenAI_API.Completions.CompletionRequest(remainder));
                            //if (output != null)
                            //{
                            //    await arg.Channel.SendMessageAsync(output);
                            //}
                        });
                        break;
                }
                if(!string.IsNullOrEmpty(reply))
                {
                    await arg.Channel.SendMessageAsync(reply);
                }
            }
        }

        bool intsAdded = false;
        private async Task _client_Ready()
        {
            textChannel = (ITextChannel)_client.GetChannel(1034557067614224454);

            if(!intsAdded)
            {
                // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
                await _intHandler.AddModulesAsync(Assembly.GetEntryAssembly(), ServiceProvider);
                await _intHandler.RegisterCommandsGloballyAsync();
                intsAdded = true;
            }
            IsConnected = true;
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
            if(_client.ConnectionState != ConnectionState.Connected)
            {
                Logger.LogWarning("Tried to post, but Discord not connected!");
                return;
            }
            if(textChannel == null)
            {
                try
                {
                    textChannel = (ITextChannel)_client.GetChannel(1034557067614224454);
                }
                catch(Exception e)
                {
                    textChannel = null;
                    Logger.LogWarning("Tried to post a message, but textChannel was null. tried to set and got exception: " + e.Message);
                }
            }
            if (textChannel != null)
            {
                await textChannel.SendMessageAsync(message);
            }
        }

        public void Dispose()
        {
            OpenAIService.Save();
            _client.Dispose();
        }
    }
}
