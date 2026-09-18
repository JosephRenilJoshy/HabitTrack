using HabitService.DTOs;

namespace HabitService.Services
{
    public interface IStreakCacheService
    {
        bool TryGet(int habitId, out StreakResponseDto? cached);
        void Set(int habitId, StreakResponseDto streak);
        void Invalidate(int habitId);
    }
}
