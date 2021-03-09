using System.Text.Json.Serialization;

namespace Groupme
{
    public class UploadResponse
    {
        [JsonPropertyName("payload")]
        public UploadPayload Payload { get; set; }
    }
}