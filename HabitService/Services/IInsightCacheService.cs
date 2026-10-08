namespace HabitService.Services
{
    public interface IInsightCacheService
    {
        bool TryGet(int habitId, out string? cachedInsight);
        void Set(int habitId, string insight);
        void Invalidate(int habitId);
    }
}
