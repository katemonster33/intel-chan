using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord.Audio;

namespace IntelChan.Bot.Discord
{
    public class DiscordAudioBuffer
    {
        AudioInStream discordStream;
        public bool LastSpeakingState = false;
        int lastWriteTime;
        MemoryStream? rawBuffer = null;
        public List<RTPFrame> cachedFrames = new List<RTPFrame>();
        public DiscordAudioBuffer(AudioInStream discordStream) : base()
        {
            this.discordStream = discordStream;
        }

        private Process? CreateFfmpegOut(string savePath, string inPath)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-ac 1 -f s32le -ar 48000 -i \"{Path.GetFullPath(inPath)}\" -ac 1 -acodec pcm_s16le -ar 16000 -f wav pipe:1",
                // Minimal version for piping etc
                //Arguments = $"-c 2 -f S16_LE -r 44100"
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }
        //int index = 0;
        public async Task<MemoryStream?> TryRead(CancellationToken cancelToken)
        {
            int currentTime = Environment.TickCount;
            if (discordStream.AvailableFrames > 0)
            {
                var buffer = new byte[3840];
                int bytesRead = 0;
                if(rawBuffer == null)
                {
                    rawBuffer = new MemoryStream();
                }
                while (discordStream.AvailableFrames > 0 && (bytesRead = await discordStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    rawBuffer.Write(buffer, 0, bytesRead);
                }
                lastWriteTime = currentTime;
            }

            if (rawBuffer != null && currentTime > lastWriteTime && (currentTime - lastWriteTime) > 1000) // roughly 2 seconds of audio
            {
                File.WriteAllBytes("Audio\\Input.raw", rawBuffer.GetBuffer());
                MemoryStream output = new MemoryStream();
                using var ffmpeg = CreateFfmpegOut($"Audio\\Output.wav", "Audio\\Input.raw");
                if (ffmpeg != null)
                {
                    //await ffmpeg.StandardOutput.BaseStream.CopyToAsync(output);
                    //ffmpeg.WaitForExit();
                    ffmpeg.StandardOutput.BaseStream.CopyTo(output);
                    //index++;
                    //output = new MemoryStream(File.ReadAllBytes("Audio\\Output.wav"));
                    //File.Delete("Audio\\Output.wav");
                }
                rawBuffer.Dispose();
                rawBuffer = null;
                return output;
            }
            return null;
        }
    }
}