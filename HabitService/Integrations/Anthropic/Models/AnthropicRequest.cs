using System.Text.Json.Serialization;

namespace HabitService.Integrations.Anthropic.Models
{
    public class AnthropicRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("messages")]
        public List<AnthropicMessage> Messages { get; set; } = new List<AnthropicMessage>();

    }

    public class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
