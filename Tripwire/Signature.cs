using System.Text.Json.Serialization;

namespace Tripwire
{
    public class Signature
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("signatureID")]
        public string SignatureID { get; set; }

        [JsonPropertyName("systemID")]
        public string SystemID { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("bookmark")]
        public string Bookmark { get; set; }

        [JsonPropertyName("lifeTime")]
        public string LifeTime { get; set; }

        [JsonPropertyName("lifeLeft")]
        public string LifeLeft { get; set; }

        [JsonPropertyName("lifeLength")]
        public string LifeLength { get; set; }

        [JsonPropertyName("createdByID")]
        public string CreatedByID { get; set; }

        [JsonPropertyName("createdByName")]
        public string CreatedByName { get; set; }

        [JsonPropertyName("modifiedByID")]
        public string ModifiedByID { get; set; }

        [JsonPropertyName("modifiedByName")]
        public string ModifiedByName { get; set; }

        [JsonPropertyName("modifiedTime")]
        public string ModifiedTime { get; set; }

        [JsonPropertyName("maskID")]
        public string MaskID { get; set; }

        public string SystemName {get;set;}
    }
}