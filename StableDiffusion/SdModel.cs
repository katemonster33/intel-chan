using System.Text.Json.Serialization;

namespace IntelChan.StableDiffusion
{
    public class SdModel
    {
        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Title { get; set; } = "";

        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = "";

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = "";

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = "";

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";

        [JsonPropertyName("config")]
        public string Config { get; set; } = "";
    }
}
