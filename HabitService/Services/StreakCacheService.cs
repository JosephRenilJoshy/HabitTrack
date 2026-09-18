using HabitService.DTOs;
using System.Collections.Concurrent;

namespace HabitService.Services
{
    public class StreakCacheService : IStreakCacheService
    {
        private readonly ConcurrentDictionary<int, (StreakResponseDto Streak, DateTime CachedAt)> _cache = new();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

        public void Invalidate(int habitId)
        {
            _cache.TryRemove(habitId, out _);
        }

        public void Set(int habitId, StreakResponseDto streak)
        {
            _cache[habitId] = (streak, DateTime.UtcNow);
        }

        public bool TryGet(int habitId, out StreakResponseDto? cached)
        {
            if(_cache.TryGetValue(habitId, out var entry) 
                && DateTime.UtcNow - entry.CachedAt < CacheDuration)
            {
                cached = entry.Streak;
                return true;
            }

            cached = null;
            return false;
        }
    }
}
