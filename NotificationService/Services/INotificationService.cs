using NotificationService.Models;

namespace NotificationService.Services
{
    public interface INotificationService
    {
        Task CreateAtRiskNotificationAsync(int userId, int habitId, string habitName);

        Task<List<Notification>> GetForUserAsync(int userId);

        Task CreateMilestoneNotificationAsync(int userId, int habitId, string habitName, int milestone);
    }
}
