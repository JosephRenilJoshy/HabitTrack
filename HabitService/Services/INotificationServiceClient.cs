namespace HabitService.Services
{
    public interface INotificationServiceClient
    {
        Task NotifyMilestoneAsync(int userId, int habitId, string habitName, int milestone);
    }
}
