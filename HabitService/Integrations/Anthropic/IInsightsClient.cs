using HabitService.DTOs;

namespace HabitService.Integrations.Anthropic
{
    public interface IInsightsClient
    {
        Task<InsightResult?> GenerateInsightAsync(HabitInsightSummary summary, CancellationToken cancellationToken); 
    }
}
