using HabitService.DTOs;
using HabitService.Integrations.Anthropic.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;

namespace HabitService.Integrations.Anthropic
{
    public class AnthropicInsightsClient : IInsightsClient
    {
        private readonly HttpClient _httpClient;
        private readonly AnthropicOptions _options;
        private readonly ILogger<AnthropicInsightsClient> _logger;

        public AnthropicInsightsClient(HttpClient httpClient, IOptions<AnthropicOptions> options, ILogger<AnthropicInsightsClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }
        public async Task<InsightResult?> GenerateInsightAsync(HabitInsightSummary summary, CancellationToken cancellationToken)
        {
            var prompt = BuildPrompt(summary);

            var requestBody = new AnthropicRequest
            {
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                Messages = new List<AnthropicMessage>
                {
                    new AnthropicMessage { Role = "user", Content = prompt }
                }
            };

            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

                    var response = await _httpClient.PostAsJsonAsync("v1/messages", requestBody, timeoutCts.Token);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Rate limited by Anthropic API. Attempt {Attempt}/{Max}", attempt, maxAttempts);
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Anthropic API returned {StatusCode}: {Body}", response.StatusCode, errorBody);
                        return null;
                    }

                    var parsed = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: cancellationToken);
                    return ExtractInsight(parsed);
                }
                catch(OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Request to Anthropic API timed out. Attempt {Attempt}/{Max}", attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error calling Anthropic API.");
                    return null;
                }
            }

            return null;
        }

        private InsightResult? ExtractInsight(AnthropicResponse? response)
        {
            var rawText = response?.Content.FirstOrDefault(c => c.Type == "text")?.Text;

            if(string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("No text content found in the response.");
                return null;
            }

            try
            {
                var cleaned = rawText.Trim();
                if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Trim('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();
                }
                return JsonSerializer.Deserialize<InsightResult>(cleaned);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize the insight result from the response.");
                return null;
            }
        }

        private static string BuildPrompt(HabitInsightSummary s)
        {
            var targetLine = s.TargetPerWeek.HasValue ? $" (target: {s.TargetPerWeek} per week)" : "";
            var missedDays = s.MostMissedWeekDays.Count > 0 ? string.Join(", ", s.MostMissedWeekDays) : "no clear pattern";

            return $$"""
                You are a supportive habit-tracking assistant. Based on this user's habit data, write ONE short, encouraging insight or suggestion (2-3 sentences max). Be specific and reference the actual numbers. Do not be generic.

                Habit: {{s.HabitName}}
                Frequency: {{s.FrequencyType}}{{targetLine}}
                Current streak: {{s.CurrentStreak}}
                Longest streak: {{s.LongestStreak}}
                Check-ins in the last 30 days: {{s.CheckInsLast30Days}}
                Completion rate (last 30 days): {{s.CompletionRateLast30Days:P0}}
                Most often missed on: {{missedDays}}

                Respond with ONLY raw JSON in this exact format, no markdown, no extra text:
                { "insight": "<your 2-3 sentence insight here>" }
                """;
        }
    }
}
