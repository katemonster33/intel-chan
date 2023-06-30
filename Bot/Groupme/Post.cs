using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntelChan.Bot.Groupme
{
    public class Post
    {
        [JsonPropertyName("bot_id")]
        public string BotId { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("attachments")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Attachment>? Attachments { get; set; }
    }
}