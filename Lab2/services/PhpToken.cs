using System.Text.Json.Serialization;

namespace Lab2.services
{
    public class PhpToken
    {
        [JsonPropertyName("line")]
        public string line { get; set; }
        [JsonPropertyName("token_name")]
        public string TokenName { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}