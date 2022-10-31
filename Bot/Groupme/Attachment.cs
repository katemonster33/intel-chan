using System.Text.Json.Serialization;

namespace IntelChan.Bot.Groupme
{
    public class Attachment
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string URL { get; set; }

        [JsonPropertyName("lng")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Longitude { get; set; }

        [JsonPropertyName("lat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Latitude { get; set; }

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; }
    }
}