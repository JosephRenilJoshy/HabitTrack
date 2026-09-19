using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services;
using Xunit;

namespace NotificationService.Tests.Services
{
    public class NotificationServiceTests
    {
        private static NotificationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new NotificationDbContext(options);
        }

        [Fact]
        public async Task CreateMilestoneNotification_AddsNotification()
        {
            using var ctx = CreateContext(Guid.NewGuid().ToString());
            var svc = new global::NotificationService.Services.NotificationService(ctx);

            await svc.CreateMilestoneNotificationAsync(1, 2, "Pushups", 30);

            var list = await svc.GetForUserAsync(1);
            Assert.Single(list);
            var n = list.First();
            Assert.Equal(1, n.UserId);
            Assert.Equal(2, n.HabitId);
            Assert.Equal(NotificationType.Milestones, n.Type);
            Assert.Contains("Pushups", n.Message);
            Assert.Contains("30", n.Message);
        }

        [Fact]
        public async Task CreateAtRiskNotification_DeduplicatesSameDay()
        {
            using var ctx = CreateContext(Guid.NewGuid().ToString());
            var svc = new global::NotificationService.Services.NotificationService(ctx);

            await svc.CreateAtRiskNotificationAsync(5, 11, "Meditate");
            await svc.CreateAtRiskNotificationAsync(5, 11, "Meditate");

            var list = await svc.GetForUserAsync(5);
            // Only one notification should exist for the same user/habit/type on the same day
            Assert.Single(list.Where(n => n.HabitId == 11 && n.Type == NotificationType.AtRisk));
        }
    }
}
