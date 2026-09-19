using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HabitService.DTOs;
using HabitService.Models;
using HabitService.Repositories;
using HabitService.Services;
using Moq;
using Xunit;

namespace HabitService.Tests.Services
{
    public class HabitServiceTests
    {
        [Fact]
        public async Task CreateAsync_CallsRepositoryAndReturnsDto()
        {
            var repoMock = new Mock<IHabitRepository>();
            repoMock.Setup(r => r.AddAsync(It.IsAny<Habit>()))
                .Callback<Habit>(h => h.Id = 123)
                .Returns(Task.CompletedTask);
            repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var cacheMock = new Mock<IStreakCacheService>();
            var notifyMock = new Mock<INotificationServiceClient>();

            var service = new global::HabitService.Services.HabitService(repoMock.Object, cacheMock.Object, notifyMock.Object);

            var dto = new CreateHabitDto { Name = "Test Habit", FrequencyType = FrequencyType.Daily };

            var result = await service.CreateAsync(42, dto);

            Assert.Equal("Test Habit", result.Name);
            Assert.Equal(42, result.UserId);
            // Id was set by the repository callback
            Assert.Equal(123, result.Id);
            repoMock.Verify(r => r.AddAsync(It.IsAny<Habit>()), Times.Once);
            repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            var repoMock = new Mock<IHabitRepository>();
            repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Habit?)null);

            var service = new global::HabitService.Services.HabitService(repoMock.Object, Mock.Of<IStreakCacheService>(), Mock.Of<INotificationServiceClient>());

            await Assert.ThrowsAsync<HabitService.Exceptions.NotFoundException>(() => service.GetByIdAsync(1, 1));
        }

        [Fact]
        public async Task CheckInAsync_AlreadyCheckedIn_ThrowsConflict()
        {
            var habit = new Habit { Id = 1, UserId = 5, FrequencyType = FrequencyType.Daily };

            var repoMock = new Mock<IHabitRepository>();
            repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(habit);
            repoMock.Setup(r => r.HasCheckInForDateAsync(1, It.IsAny<DateOnly>())).ReturnsAsync(true);

            var service = new global::HabitService.Services.HabitService(repoMock.Object, Mock.Of<IStreakCacheService>(), Mock.Of<INotificationServiceClient>());

            await Assert.ThrowsAsync<HabitService.Exceptions.ConflictException>(() => service.CheckInAsync(1, 5, new CheckInDto()));
        }

        [Fact]
        public async Task CheckInAsync_CrossesMilestone_InvokesNotification()
        {
            var habit = new Habit { Id = 7, UserId = 9, FrequencyType = FrequencyType.Daily, Name = "Run" };

            var repoMock = new Mock<IHabitRepository>();
            repoMock.Setup(r => r.GetByIdAsync(habit.Id)).ReturnsAsync(habit);
            repoMock.Setup(r => r.HasCheckInForDateAsync(habit.Id, It.IsAny<DateOnly>())).ReturnsAsync(false);

            // Build dates: before = 6 consecutive days ending yesterday, after = those + today
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var datesBefore = Enumerable.Range(1, 6).Select(i => today.AddDays(-i)).Reverse().ToList();
            var datesAfter = datesBefore.Append(today).ToList();

            repoMock.SetupSequence(r => r.GetCheckInDatesAsync(habit.Id))
                .ReturnsAsync(datesBefore)
                .ReturnsAsync(datesAfter);

            repoMock.Setup(r => r.AddCheckInAsync(It.IsAny<CheckIn>())).Returns(Task.CompletedTask);
            repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var cacheMock = new Mock<IStreakCacheService>();
            var notifyMock = new Mock<INotificationServiceClient>();

            var service = new global::HabitService.Services.HabitService(repoMock.Object, cacheMock.Object, notifyMock.Object);

            await service.CheckInAsync(habit.Id, habit.UserId, new CheckInDto());

            cacheMock.Verify(c => c.Invalidate(habit.Id), Times.Once);
            notifyMock.Verify(n => n.NotifyMilestoneAsync(habit.UserId, habit.Id, habit.Name, 7), Times.Once);
        }
    }
}
