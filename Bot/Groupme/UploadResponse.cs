using System.Text.Json.Serialization;

namespace IntelChan.Bot.Groupme
{
    public class UploadResponse
    {
        [JsonPropertyName("payload")]
        public UploadPayload Payload { get; set; }
    }
}