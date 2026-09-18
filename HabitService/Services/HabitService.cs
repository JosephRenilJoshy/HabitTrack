using HabitService.DTOs;
using HabitService.Exceptions;
using HabitService.Models;
using HabitService.Repositories;

namespace HabitService.Services
{
    public class HabitService : IHabitService
    {
        private readonly IHabitRepository _repository;
        private readonly IStreakCacheService _cache;
        private readonly INotificationServiceClient _notificationClient;

        public HabitService(IHabitRepository repository, IStreakCacheService cache, INotificationServiceClient notificationClient)
        {
            _repository = repository;
            _cache = cache;
            _notificationClient = notificationClient;
        }
        
        public async Task<HabitResponseDto> CreateAsync(int userId, CreateHabitDto dto)
        {
            var habit = new Habit
            {
                UserId = userId,
                Name = dto.Name,
                FrequencyType = dto.FrequencyType,
                TargetPerWeek = dto.TargetPerWeek,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(habit);
            await _repository.SaveChangesAsync();

            return MapToDto(habit);
        }

        public async Task<List<HabitResponseDto>> GetByUserIdAsync(int userId)
        {
            var habits = await _repository.GetByUserIdAsync(userId);
            return habits.Select(MapToDto).ToList();
        }

        public async Task<HabitResponseDto> GetByIdAsync(int id, int userId)
        {
            var habit = await _repository.GetByIdAsync(id);

            if (habit == null || habit.UserId != userId)
            {
                // Deliberately the SAME exception/message whether the habit doesn't
                // exist at all, or exists but belongs to someone else — this avoids
                // leaking which habit IDs exist to a user probing IDs that aren't theirs
                throw new NotFoundException("Habit not found.");
            }
            return MapToDto(habit);
        }

        public async Task DeleteAsync(int id, int userId)
        {
            var habit = await _repository.GetByIdAsync(id);

            if (habit == null || habit.UserId != userId)
            {
                throw new NotFoundException("Habit not found.");
            }

            await _repository.DeleteAsync(habit);
            await _repository.SaveChangesAsync();
        }

        public async Task CheckInAsync(int habitId, int userId, CheckInDto dto)
        {
            var habit = await _repository.GetByIdAsync(habitId);

            if (habit == null || habit.UserId != userId)
            {
                throw new NotFoundException("Habit not found.");
            }

            var date = dto.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var alreadyCheckedIn = await _repository.HasCheckInForDateAsync(habitId, date);

            if (alreadyCheckedIn)
            {
                throw new ConflictException("Already checked in for this date.");
            }

            var datesBefore = await _repository.GetCheckInDatesAsync(habitId);

            var (previousStreak, _) = habit.FrequencyType == FrequencyType.Daily
                ? StreakCalculator.CalculateDailyStreak(datesBefore)
                : StreakCalculator.CalculateWeeklyTargetStreak(datesBefore, habit.TargetPerWeek ?? 1);

            var checkIn = new CheckIn
            {
                HabitId = habitId,
                Date = date,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddCheckInAsync(checkIn);
            await _repository.SaveChangesAsync();
            _cache.Invalidate(habitId); // Invalidate the cache for this habit's streak

            var datesAfter = await _repository.GetCheckInDatesAsync(habitId);
            var (newStreak, _) = habit.FrequencyType == FrequencyType.Daily
                ? StreakCalculator.CalculateDailyStreak(datesAfter)
                : StreakCalculator.CalculateWeeklyTargetStreak(datesAfter, habit.TargetPerWeek ?? 1);

            var crossedMilestones = MilestoneChecker.GetCrossedMilestone(previousStreak, newStreak);

            if(crossedMilestones.HasValue)
            {
                await _notificationClient.NotifyMilestoneAsync(userId, habitId, habit.Name, crossedMilestones.Value);
            }
        }

        public async Task<StreakResponseDto> GetStreakAsync(int habitId, int userId)
        {
            var habit = await _repository.GetByIdAsync(habitId);

            if(habit == null || habit.UserId != userId)
            {
                throw new NotFoundException("Habit not found");
            }

            var checkInDates = await _repository.GetCheckInDatesAsync(habitId);

            StreakResponseDto result;

            if (habit.FrequencyType == FrequencyType.Daily)
            {
                var (current, longest) = StreakCalculator.CalculateDailyStreak(checkInDates);
                result = new StreakResponseDto { CurrentStreak = current, LongestStreak = longest, Unit = "Days" };
            }
            else
            {
                var (current, longest) = StreakCalculator.CalculateWeeklyTargetStreak(
                    checkInDates, habit.TargetPerWeek ?? 1);
                result = new StreakResponseDto { CurrentStreak = current, LongestStreak = longest, Unit = "weeks" };
            }
            _cache.Set(habitId, result); // Cache the result for future requests

            return result;
        }

        private static HabitResponseDto MapToDto(Habit habit)
        {
            return new HabitResponseDto
            {
                Id = habit.Id,
                UserId = habit.UserId,
                Name = habit.Name,
                FrequencyType = habit.FrequencyType.ToString(),
                TargetPerWeek = habit.TargetPerWeek,
                CreatedAt = habit.CreatedAt
            };
        }

        // This is the "bad" implementation that does N+1 queries
        public async Task<List<HabitWithCountDto>> GetHabitWithCountsBadAsync(int userId)
        {
            var habits = await _repository.GetAllByUserIdAsync(userId);

            var result = new List<HabitWithCountDto>();

            foreach(var habit in habits)
            {
                var checkInCount = (await _repository.GetCheckInDatesAsync(habit.Id)).Count;
                result.Add(new HabitWithCountDto
                {
                    Id = habit.Id,
                    Name = habit.Name,
                    CheckInCount = checkInCount
                });
            }
            return result;
        }

        public async Task<List<HabitWithCountDto>> GetHabitWithCountsAsync(int userId)
        {
            var habitsWithCounts = await _repository.GetAllByUserIdWithCountsAsync(userId);
            return habitsWithCounts.Select(hwc => new HabitWithCountDto
            {
                Id = hwc.habit.Id,
                Name = hwc.habit.Name,
                CheckInCount = hwc.CheckInCount
            }).ToList();
        }

        public async Task<List<HabitResponseDto>> GetAtRiskHabitsAsync()
        {
            var allHabits = await _repository.GetAllHabitsAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var atRisk = new List<Habit>();

            foreach(var habit in allHabits)
            {
                var hasCheckedInToday = await _repository.HasCheckInForDateAsync(habit.Id, today);
                if(!hasCheckedInToday)
                {
                    atRisk.Add(habit);
                }
            }
            return atRisk.Select(MapToDto).ToList();
        }
    }
}
