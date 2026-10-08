using System.Text.Json.Serialization;

namespace HabitService.Integrations.Anthropic.Models
{
    public class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContentBlock> Content { get; set; } = new List<AnthropicContentBlock>();

        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }
    }

    public class AnthropicContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
