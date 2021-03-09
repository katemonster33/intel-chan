using System.Text.Json.Serialization;

namespace Groupme
{
    public class UploadPayload
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("picture_url")]
        public string PictureUrl { get; set; }
    }
}