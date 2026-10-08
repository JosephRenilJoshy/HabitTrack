namespace HabitService.Services
{
    public interface IHabitInsightService
    {
        Task<string> GetInsightAsync(int habitId, int userId, CancellationToken cancellationToken);
    }
}
