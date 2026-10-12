using HabitService.Data;
using HabitService.Models;
using HabitService.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserService.Data;

namespace MilestoneFunction;

public class MilestoneCheckFunction
{
    private readonly ILogger<MilestoneCheckFunction> _logger;
    private readonly HabitDbContext _habitDb;
    private readonly UserDbContext _userDb;

    public MilestoneCheckFunction(
        ILogger<MilestoneCheckFunction> logger,
        HabitDbContext habitDb,
        UserDbContext userDb)
    {
        _logger = logger;
        _habitDb = habitDb;
        _userDb = userDb;
    }

    [Function("MilestoneCheckFunction")]
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        var habits = await _habitDb.Habits.Include(h => h.CheckIns).ToListAsync();
        var milestonesFound = 0;

        foreach(var habit in habits)
        {
            var dates = habit.CheckIns.Select(c => c.Date).ToList();

            var (current, _) = habit.FrequencyType == FrequencyType.Daily
                ? StreakCalculator.CalculateDailyStreak(dates)
                : StreakCalculator.CalculateWeeklyTargetStreak(dates, habit.TargetPerWeek ?? 1);

            var milestone = MilestoneChecker.GetCrossedMilestone(habit.LastCheckedStreak, current);

            if (milestone is not null)
            {
                var user = await _userDb.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == habit.UserId);

                _logger.LogInformation(
                    "Milestone {Milestone} reached: habit '{Habit}' (id {HabitId}) for user {Email}",
                    milestone, habit.Name, habit.Id, user?.Email ?? "unknown");

                milestonesFound++;
            }

            habit.LastCheckedStreak = current;
        }

        await _habitDb.SaveChangesAsync();

        _logger.LogInformation("Checked {Count} habits, {Found} milestone(s) found at {Time}",
            habits.Count, milestonesFound, DateTime.UtcNow);
    }
}