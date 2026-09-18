using HabitService.DTOs;

namespace HabitService.Services
{
    public interface IHabitService
    {
        Task<HabitResponseDto> CreateAsync(int userId, CreateHabitDto dto);
        Task<List<HabitResponseDto>> GetByUserIdAsync(int userId);
        Task<HabitResponseDto> GetByIdAsync(int id, int userId);
        Task DeleteAsync(int id, int userId);
        Task CheckInAsync(int habitId, int userId, CheckInDto dto);
        Task<StreakResponseDto> GetStreakAsync(int habitId, int userId);
        Task<List<HabitWithCountDto>> GetHabitWithCountsBadAsync(int userId);
        Task<List<HabitWithCountDto>> GetHabitWithCountsAsync(int userId);
        Task<List<HabitResponseDto>> GetAtRiskHabitsAsync();
    }
}
