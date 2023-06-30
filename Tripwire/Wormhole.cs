using System.Text.Json.Serialization;

namespace Tripwire
{
    public class Wormhole
    {
        [JsonPropertyName("initialID")]
        public string InitialID { get; set; } = "";

        [JsonPropertyName("secondaryID")]
        public string SecondaryID { get; set; } = "";

        [JsonPropertyName("parent")]
        public string Parent { get; set; } = "";

        [JsonPropertyName("maskID")]
        public string MaskID { get; set; } = "";


        public string? GetParentId()
        {
            if(Parent == "initial")
            {
                return InitialID;
            }
            else if(Parent == "secondary")
            {
                return SecondaryID;
            }
            return null;
        }
        

        public string? GetChildId()
        {
            if(Parent == "initial")
            {
                return SecondaryID;
            }
            else if(Parent == "secondary")
            {
                return InitialID;
            }
            return null;
        }
    }
}