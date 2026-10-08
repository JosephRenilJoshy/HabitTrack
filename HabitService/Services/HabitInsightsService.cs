using HabitService.Exceptions;
using HabitService.Integrations.Anthropic;
using HabitService.Models;
using HabitService.Repositories;

namespace HabitService.Services
{
    public class HabitInsightsService : IHabitInsightService
    {
        private const string FallbackMessage = "Insight temporarily unavailable. Check back soon!";

        private readonly IHabitRepository _habitRepository;
        private readonly IInsightsClient _insightsClient;
        private readonly IInsightCacheService _cache;
        private readonly ILogger<HabitInsightsService> _logger;

        public HabitInsightsService(IHabitRepository habitRepository, IInsightsClient insightsClient, IInsightCacheService cache, ILogger<HabitInsightsService> logger)
        {
            _habitRepository = habitRepository;
            _insightsClient = insightsClient;
            _cache = cache;
            _logger = logger;
        }
        public async Task<string> GetInsightAsync(int habitId, int userId, CancellationToken cancellationToken)
        {
            var habit = await _habitRepository.GetByIdAsync(habitId);

            if(habit == null || habit.UserId != userId)
            {
                throw new NotFoundException("Habit not found.");
            }

            // Cache check happens AFTER ownership validation - never let a cache
            // hit accidentally skip the authorization check
            if (_cache.TryGet(habitId, out var cachedInsight))
            {
                return cachedInsight!;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var thirtyDaysAgo = today.AddDays(-30);

            var alldates = await _habitRepository.GetCheckInDatesAsync(habitId);
            var last30Days = await _habitRepository.GetCheckInDatesInRangeAsync(habitId, thirtyDaysAgo, today);

            var (currentStreak, longestStreak) = habit.FrequencyType == FrequencyType.Daily 
                ? StreakCalculator.CalculateDailyStreak(alldates)
                : StreakCalculator.CalculateWeeklyTargetStreak(alldates, habit.TargetPerWeek ?? 1);

            var summary = InsightAggregator.BuildSummary(habit, alldates, last30Days, currentStreak, longestStreak);

            var result = await _insightsClient.GenerateInsightAsync(summary, cancellationToken);

            if (result == null || string.IsNullOrWhiteSpace(result.Insight))
            {
                _logger.LogWarning("Insight generation failed for habit {HabitId}. Returning fallback message.", habitId);
                return FallbackMessage;
            }

            _cache.Set(habitId, result.Insight);
            return result.Insight;
        }
    }
}
