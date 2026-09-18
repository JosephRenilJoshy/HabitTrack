using NotificationService.DTOs;

namespace NotificationService.Services
{
    public interface IHabitServiceClient
    {
        Task<List<HabitDto>> GetAtRiskHabitsAsync();
    }
}
