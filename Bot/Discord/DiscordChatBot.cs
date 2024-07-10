using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.WebSocket;
using IntelChan.VoiceChatter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Tripwire;
//using OpenAI_API;
using OpenAI.Chat;
using Discord.Audio;
using NAudio.Wave;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Discord.Audio.Streams;
using OpenAI;
using IntelChan.OpenAI;
using Discord.Interactions;
using System.Reflection;

namespace IntelChan.Bot.Discord
{
    public class DiscordChatBot : IChatBot
    {
        DiscordSocketClient _client;
        InteractionService _intHandler;

        string? discordBotToken;

        ITextChannel? textChannel;
        IVoiceChannel? activeVoiceChannel;
        IAudioClient? activeAudioClient;
        CancellationTokenSource voiceChannelToken = new CancellationTokenSource();


        public event Func<string, Task<string>>? HandlePathCommand;
        public event Func<string, byte[]?, Task<string>>? HandleDrawCommand;
        public event Func<Task<List<string>>>? HandleGetModelsCommand;
        public event Func<string, Task<bool>>? HandleSetModelCommand;

        ElevenLabs ElevenLabsClient { get; }

        IConfiguration Config { get; }

        ILogger<DiscordChatBot> Logger { get; }
        //OpenAIAPI OpenAIAPI { get; }
        ChatClient OpenAiClient { get; }
        OpenAIConfig openAIConfig { get; }

        public DiscordChatBot(IConfiguration config, ILogger<DiscordChatBot> logger, ElevenLabs elevenLabs)
        {
            Config = config;
            Logger = logger;
            ElevenLabsClient = elevenLabs;
            //OpenAIAPI = new OpenAIAPI(Config["openai-key"] ?? string.Empty);
            OpenAiClient = new ChatClient("gpt-4", Config["openai-key"] ?? string.Empty, new());
            if(File.Exists("OpenAiCfg.json"))
            {
                openAIConfig = OpenAIConfig.LoadFromFile("OpenAiCfg.json");
            }
            else openAIConfig = new OpenAIConfig();
            
            _client = new DiscordSocketClient(new DiscordSocketConfig(){GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates | GatewayIntents.Guilds});

            _client.Log += Log;

            _client.Ready += _client_Ready;

            _client.SlashCommandExecuted += Client_SlashCommandExecuted;

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
                var result = await _intHandler.ExecuteCommandAsync(context, null);

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

        void OutputSpeechRecognitionResult(SpeechRecognitionResult speechRecognitionResult)
        {
            switch (speechRecognitionResult.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    Logger.LogInformation($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                    break;
                case ResultReason.NoMatch:
                    Logger.LogWarning($"NOMATCH: Speech could not be recognized.");
                    break;
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                    string warning = $"CANCELED: Reason={cancellation.Reason}";

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        warning += $", ErrorCode={cancellation.ErrorCode}, ErrorDetails={cancellation.ErrorDetails}: Did you set the speech resource key and region values?";
                    }
                    Logger.LogWarning(warning);
                    break;
                default:
                    Logger.LogError("Unrecognized result: " + speechRecognitionResult.Reason.ToString());
                    break;
            }
        }

        async void HandleVoiceCall()
        {
            if(activeVoiceChannel == null)
            {
                return;
            }
            activeAudioClient = await activeVoiceChannel.ConnectAsync();
            var speechConfig = SpeechConfig.FromSubscription(Config["azure-key-1"], "eastus");
            speechConfig.SpeechRecognitionLanguage = "en-US";
            Dictionary<ulong, DiscordAudioBuffer> audioBuffers = new Dictionary<ulong, DiscordAudioBuffer>();
            foreach (var pair in activeAudioClient.GetStreams())
            {
                audioBuffers[pair.Key] = new DiscordAudioBuffer(pair.Value); //DiscordToAzureSpeechConverter.Create(pair.Value, speechConfig, voiceChannelToken.Token);
            }
            activeAudioClient.StreamCreated += (o, i) =>
            {
                return Task.Run(() =>
                {
                    lock (audioBuffers)
                    {
                        audioBuffers[o] = new DiscordAudioBuffer(i); //DiscordToAzureSpeechConverter.Create(i, speechConfig, voiceChannelToken.Token);
                    }
                });
            };

            activeAudioClient.StreamDestroyed += (o) =>
            {
                return Task.Run(() =>
                {
                    lock (audioBuffers)
                    {
                        audioBuffers.Remove(o);
                    }
                });
            };

            //var output = await MetaVoice.RequestAudio("Hello everyone. I am here.", "output");
            //if (output != null)
            //{
                await SpeakAudio(activeAudioClient, "ding.mp3");
            //}

            while (!voiceChannelToken.IsCancellationRequested)
            {
                MemoryStream? memoryStream = null;
                foreach(var buffer in new List<DiscordAudioBuffer>(audioBuffers.Values))
                {
                    
                    if ((memoryStream = await buffer.TryRead(voiceChannelToken.Token)) != null)
                    {
                        break;
                    }
                }
                if (memoryStream != null)
                {
                    memoryStream.Position = 0;
                    DiscordAudioPullStreamCallback pullStreamCallback = new DiscordAudioPullStreamCallback(memoryStream);
                    using var audioConfig = AudioConfig.FromStreamInput(AudioInputStream.CreatePullStream(pullStreamCallback));
                    using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
                    var result = await speechRecognizer.RecognizeOnceAsync();
                    OutputSpeechRecognitionResult(result);
                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        string? prompt = null;
                        if (result.Text.StartsWith("OK Intel"))
                        {
                            prompt = result.Text.Substring(8);
                        }
                        else if (result.Text.StartsWith("OK, Intel"))
                        {
                            prompt = result.Text.Substring(9);
                        }
                        if(prompt != null)
                        {
                            if(!openAIConfig.TryGetContextMessages(activeVoiceChannel.Id.ToString(), out var list))
                            {
                                list = new List<ChatMessage>();
                            }
                            list.Add(new UserChatMessage(prompt));
                            ChatCompletion comp = OpenAiClient.CompleteChat(list, new ChatCompletionOptions());
                            var output = await OpenVoice.RequestAudio(comp.ToString(), "output");
                            if (output != null)
                            {
                                await SpeakAudio(activeAudioClient, output);
                            }
                        }
                    }
                }
                Thread.Sleep(1);
            }
            await activeVoiceChannel.DisconnectAsync();
            activeAudioClient.Dispose();
            activeVoiceChannel = null;
        }

        bool TryGetVoiceChannel(string remainder, out IVoiceChannel? voiceChannel)
        {
            voiceChannel = null;
            if (remainder.StartsWith("https://discord.com/channels/") || remainder.StartsWith("https://discordapp.com/channels/"))
            {
                string[] urlsplit = remainder.Split('/');
                if (urlsplit.Length == 6 && ulong.TryParse(urlsplit[4], out ulong guildId) && ulong.TryParse(urlsplit[5], out ulong channelId))
                {
                    var guild = _client.GetGuild(guildId);
                    if (guild != null)
                    {
                        var channel = guild.GetVoiceChannel(channelId);
                        if (channel != null)
                        {
                            voiceChannel = channel;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        Task? voiceCallTask = null;
        async Task _client_MessageReceived(SocketMessage arg)
        {
            if (arg == null) throw new ArgumentNullException(nameof(arg));
            string cc = arg.Content;
            if (arg.Author is SocketGuildUser sgi)
            {
                if (sgi.DisplayName.Contains("Oisan") || sgi.DisplayName.Contains("ois_in"))
                //if(sgi.Id == 381709342853824513)
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

                    case "joincall":
                        if (TryGetVoiceChannel(remainder, out var voiceChannel))
                        {
                            if (activeVoiceChannel != null)
                            {
                                await arg.Channel.SendMessageAsync("Already in channel!");
                            }
                            else
                            {
                                activeVoiceChannel = voiceChannel;
                                voiceCallTask = Task.Run(HandleVoiceCall);
                            }
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Unable to locate voice channel. Do I have permissions to see it?");
                        }
                        break;

                    case "leavecall":
                        if (voiceCallTask != null)
                        {
                            voiceChannelToken.Cancel();
                            await voiceCallTask;
                            voiceCallTask = null;
                        }
                        break;

                    case "startcharacter":
                        if(openAIConfig.StartCharacter(arg.Channel.Id.ToString(), new SystemChatMessage(remainder)))
                        {
                            await arg.Channel.SendMessageAsync("Started character successfully.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Couldn't create the character - is there a character already active in this channel?");
                        }
                        break;

                    case "stopcharacter":
                        if(openAIConfig.StopCharacter(arg.Channel.Id.ToString()))
                        {
                            await arg.Channel.SendMessageAsync("Success, character stopped.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Failed, no character session appears active in this channel.");
                        }
                        break;

                    case "savecharacter":
                        if(openAIConfig.SaveCharacter(arg.Channel.Id.ToString(), remainder))
                        {
                            await arg.Channel.SendMessageAsync($"Success, character \'{remainder}\' saved.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Couldn't save the character. Is there an active session?");
                        }
                        break;

                    case "loadcharacter":
                        if(openAIConfig.LoadCharacter(arg.Channel.Id.ToString(), remainder))
                        {
                            await arg.Channel.SendMessageAsync($"Success, character \'{remainder}\' loaded to the active session.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Couldn't load the character to the current channel. Does it exist?");
                        }
                        break;

                        case "getcharacters":
                        List<string> characters = openAIConfig.GetCharacters();

                        await arg.Channel.SendMessageAsync($"List of saved characters: [{string.Join(",", characters)}]");
                        break;


                    case "prunecharacter":
                        int numMessages = -1;
                        if(!string.IsNullOrEmpty(remainder))
                        {
                            int.TryParse(remainder, out numMessages);
                        }
                        if(openAIConfig.Prune(arg.Channel.Id.ToString(), numMessages))
                        {
                            await arg.Channel.SendMessageAsync($"Success, pruned the character log.");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("Couldn't prune any messages. Does the character exist? Have you spoken to it yet?");
                        }
                        break;

                    case "say":
                        _ = Task.Run(async () =>
                        {
                            //var output = await ElevenLabsClient.RequestAudio(remainder, "neWi1tdhjirZA1DtRQ0I", "output.mp3");

                            await SpeakAudio(arg.Channel, remainder);
                        });
                        break;

                    case "ask":
                        _ = Task.Run(async () =>
                        {
                            if(!openAIConfig.TryGetContextMessages(arg.Channel.Id.ToString(), out var list))
                            {
                                list = new List<ChatMessage>();
                            }
                            list.Add(new UserChatMessage(remainder));
                            ChatCompletion comp = OpenAiClient.CompleteChat(list, new ChatCompletionOptions());
                            if (comp != null)
                            {
                                Logger.LogInformation("OpenAI said: " + comp.ToString());
                                await arg.Channel.SendMessageAsync(comp.ToString());
                                //var output = await ElevenLabsClient.RequestAudio(comp.ToString(), "neWi1tdhjirZA1DtRQ0I", "output.mp3");
                                //await SpeakAudio(arg.Channel, comp.ToString());
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

        private async Task SpeakAudio(ISocketMessageChannel channel, string prompt)
        {
            var output = await OpenVoice.RequestAudio(prompt, "output");
            if (output != null)
            {
                if (activeAudioClient != null)
                {
                    await SpeakAudio(activeAudioClient, output);
                }
                else
                {
                    await channel.SendFileAsync(Path.GetFullPath(output));
                }
                //File.Delete(Path.GetFullPath(output));
                //await channel.SendMessageAsync(comp.ToString());
            }
        }

        private async Task SpeakAudio(IAudioClient client, string output)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{Path.GetFullPath(output)}"" -ac 2 -f s16le -ar 48000 pipe:1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                using var ffmpeg = Process.Start(psi);
                if (ffmpeg != null)
                {
                    var ffmpegStream = ffmpeg.StandardOutput.BaseStream;
                    using var discord = client.CreatePCMStream(AudioApplication.Voice);
                    await ffmpegStream.CopyToAsync(discord);
                    await discord.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }

        private async Task Client_SlashCommandExecuted(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "draw":
                    if(HandleDrawCommand != null)
                    {
                        string prompt = (string)command.Data.Options.First().Value;
                        await HandleDrawCommand.Invoke(prompt, null);
                    }
                    break;
            }
        }
        bool intsAdded = false;
        private async Task _client_Ready()
        {
            textChannel = (ITextChannel)_client.GetChannel(1034557067614224454);

            if(!intsAdded)
            {
                // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
                await _intHandler.AddModulesAsync(Assembly.GetEntryAssembly(), null);
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
            openAIConfig.Save("OpenAiCfg.json");
            _client.Dispose();
        }
    }
}
