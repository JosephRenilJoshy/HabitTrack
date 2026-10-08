using HabitService.Models;

namespace HabitService.Repositories
{
    public interface IHabitRepository
    {
        Task<Habit?> GetByIdAsync(int id);
        Task<Habit?> GetByIdWithCheckInsAsync(int id);
        Task<List<Habit>> GetByUserIdAsync(int userId);
        Task AddAsync(Habit habit);
        Task UpdateAsync(Habit habit);
        Task DeleteAsync(Habit habit);
        Task<bool> HasCheckInForDateAsync(int habitId, DateOnly date);
        Task AddCheckInAsync(CheckIn checkIn);
        Task SaveChangesAsync();
        Task<List<DateOnly>> GetCheckInDatesAsync(int habitId);

        Task<List<Habit>> GetAllByUserIdAsync(int userId);
        Task<List<Habit>> GetAllHabitsAsync();
        Task<List<(Habit habit, int CheckInCount)>> GetAllByUserIdWithCountsAsync(int userId);
        Task<List<DateOnly>> GetCheckInDatesInRangeAsync(int habitId, DateOnly startDate, DateOnly fromDate);
    }
}
