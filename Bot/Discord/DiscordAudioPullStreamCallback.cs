using System.IO;
using Microsoft.CognitiveServices.Speech.Audio;

namespace IntelChan.Bot.Discord
{
    public class DiscordAudioPullStreamCallback : PullAudioInputStreamCallback
    {
        MemoryStream buffer;
        public DiscordAudioPullStreamCallback(MemoryStream buffer) : base()
        {
            this.buffer = buffer;
        }

        public override int Read(byte[] dataBuffer, uint size)
        {
            return buffer.Read(dataBuffer, 0, (int)size);
        }
    }
}