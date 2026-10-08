using System.Collections.Concurrent;

namespace HabitService.Services
{
    public class InsightCacheService : IInsightCacheService
    {
        private readonly ConcurrentDictionary<int, (string Insight, DateTime CachedAt)> _cache = new();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

        public bool TryGet(int habitId, out string? cachedInsight)
        {
            if (_cache.TryGetValue(habitId, out var entry) &&
                DateTime.UtcNow - entry.CachedAt < CacheDuration)
            {
                cachedInsight = entry.Insight;
                return true;
            }

            cachedInsight = null;
            return false;
        }

        public void Set(int habitId, string insight)
        {
            _cache[habitId] = (insight, DateTime.UtcNow);
        }

        public void Invalidate(int habitId)
        {
            _cache.TryRemove(habitId, out _);
        }
    }
}
