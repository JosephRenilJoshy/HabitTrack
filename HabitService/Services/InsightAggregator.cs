using HabitService.DTOs;
using HabitService.Models;

namespace HabitService.Services
{
    public static class InsightAggregator
    {
        public static HabitInsightSummary BuildSummary(
            Habit habit,
            List<DateOnly> allCheckInDates,
            List<DateOnly> last30DaysCheckInDates,
            int currentStreak,
            int longestStreak)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var thirtyDaysAgo = today.AddDays(-30);

            var expectedDaysInWindow = today.DayNumber - thirtyDaysAgo.DayNumber;
            var completionRate = expectedDaysInWindow > 0 ? 
                (double)last30DaysCheckInDates.Count / expectedDaysInWindow 
                : 0;

            var mostMissedWeekDays = FindMostMissedWeekDays(allCheckInDates, thirtyDaysAgo, today);

            return new HabitInsightSummary
            {
                HabitName = habit.Name,
                FrequencyType = habit.FrequencyType.ToString(),
                TargetPerWeek = habit.TargetPerWeek,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                TotalCheckIns = allCheckInDates.Count,
                CheckInsLast30Days = last30DaysCheckInDates.Count,
                CompletionRateLast30Days = completionRate,
                MostMissedWeekDays = mostMissedWeekDays
            };

        }

        private static List<string> FindMostMissedWeekDays(
            List<DateOnly> checkInDates, DateOnly fromdate, DateOnly toDate)
        {
            var checkInSet = checkInDates.ToHashSet();

            var missesByWeekday = new Dictionary<DayOfWeek, int>();
            var occurrencesByWeekday = new Dictionary<DayOfWeek, int>();

            for(var date = fromdate; date <= toDate; date = date.AddDays(1))
            {
                var weekday = date.DayOfWeek;
                occurrencesByWeekday[weekday] = occurrencesByWeekday.GetValueOrDefault(weekday) + 1;
                if (!checkInSet.Contains(date))
                {
                    missesByWeekday[weekday] = missesByWeekday.GetValueOrDefault(weekday) + 1;
                }
            }

            var significantlyMissed = missesByWeekday
                .Where(kvp => kvp.Value > occurrencesByWeekday[kvp.Key] / 2.0)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key.ToString())
                .Take(2)
                .ToList();

            return significantlyMissed;
        }
    }
}
