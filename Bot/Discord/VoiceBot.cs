using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;
using IntelChan.OpenAI;
using IntelChan.VoiceChatter;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace IntelChan.Bot.Discord
{
    public class VoiceBotCommands : InteractionModuleBase<SocketInteractionContext>
    {
        CancellationTokenSource voiceChannelToken = new CancellationTokenSource();
        IVoiceChannel? activeVoiceChannel;
        IAudioClient? activeAudioClient;
        Task? voiceCallTask = null;

        OpenAIService OpenAIService { get; }
        OpenVoiceService OpenVoiceService { get; }
        ILogger<VoiceBotCommands> Logger { get; }
        IConfiguration Config { get; }

        public VoiceBotCommands(IConfiguration config, ILogger<VoiceBotCommands> logger, OpenAIService openAIService, OpenVoiceService openVoiceService)
        {
            Config = config;
            OpenAIService = openAIService;
            OpenVoiceService = openVoiceService;
            Logger = logger;
        }

        [SlashCommand("joincall", "Join a Discord voice channel to begin speaking with people using Speech->Text->Speech")]
        public async Task JoinCall(IVoiceChannel voiceChannel)
        {
            if (activeVoiceChannel != null)
            {
                await RespondAsync("Already in channel!");
            }
            else
            {
                activeVoiceChannel = voiceChannel;
                voiceCallTask = Task.Run(HandleVoiceCall);
                await RespondAsync("Success, joined channel.");
            }
        }

        [SlashCommand("leavecall", "Disconnect from the currently-connected Discord voice call.")]
        public async Task LeaveCall()
        {
            if (voiceCallTask != null)
            {
                voiceChannelToken.Cancel();
                await voiceCallTask;
                voiceCallTask = null;
                await RespondAsync("OK, left channel.");
            }
            else
            {
                await RespondAsync("Not currently in a voice channel, bro :\\");
            }
        }

        [SlashCommand("say", "Speak the desired audio using text-to-speech", runMode: RunMode.Async)]
        public async Task Say(string text)
        {
            await DeferAsync();
            string? fileName = await OpenVoiceService.RequestAudio(text, "output");
            if (fileName != null)
            {
                await FollowupWithFileAsync(Path.GetFullPath(fileName));
                //File.Delete(Path.GetFullPath(output));
                //await channel.SendMessageAsync(comp.ToString());
            }
            else
            {
                await FollowupAsync("OpenVoice did not respond with valid speech. Maybe a temporary bug?");
            }
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
                            if(!OpenAIService.CompleteChat(activeVoiceChannel.Id.ToString(), prompt, out string? resp))
                            {
                                resp = "Did not get a response from OpenAI.";
                            }
                            var output = await OpenVoiceService.RequestAudio(resp, "output");
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
    }
}