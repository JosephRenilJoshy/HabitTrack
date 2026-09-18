using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;

namespace NotificationService.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationDbContext _context;

        public NotificationService(NotificationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAtRiskNotificationAsync(int userId, int habitId, string habitName)
        {
            var today = DateTime.UtcNow.Date;
            var alreadyNotifiedToday = await _context.Notifications.AnyAsync(n =>
                n.UserId == userId &&
                n.HabitId == habitId &&
                n.Type == NotificationType.AtRisk &&
                n.CreatedAt.Date == today
            );

            if (alreadyNotifiedToday)
            {
                return;
            }

            var notification = new Notification
            {
                UserId = userId,
                HabitId = habitId,
                Type = NotificationType.AtRisk,
                Message = $"Don't forget to complete your habit: {habitName} today!",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateMilestoneNotificationAsync(int userId, int habitId, string habitName, int milestone)
        {
            var notification = new Notification
            {
                UserId = userId,
                HabitId = habitId,
                Type = NotificationType.Milestones,
                Message = $"Congratulations! You've reached a milestone in your habit: {habitName} ({milestone})!",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetForUserAsync(int userId)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }
}
