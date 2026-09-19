using HabitService.Services;
using Xunit;

namespace HabitService.Tests.Services
{
    public class StreakCalculatorTests
    {
        [Fact]
        public void CalculateDailyStreak_WithNoCheckIns_ReturnsZeroZero()
        {
            var result = StreakCalculator.CalculateDailyStreak(new List<DateOnly>());

            Assert.Equal(0, result.Current);
            Assert.Equal(0, result.Longest);
        }

        [Fact]
        public void CalculateDailyStreak_WithSingleCheckInToday_ReturnsOneOne()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var result = StreakCalculator.CalculateDailyStreak(new List<DateOnly> { today });

            Assert.Equal(1, result.Current);
            Assert.Equal(1, result.Longest);
        }

        [Fact]
        public void CalculateDailyStreak_WithConsecutiveDays_ReturnsCorrectLongestStreak()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dates = new List<DateOnly>
            {
                today.AddDays(-4),
                today.AddDays(-3),
                today.AddDays(-2),
                today.AddDays(-1),
                today
            };

            var result = StreakCalculator.CalculateDailyStreak(dates);

            Assert.Equal(5, result.Current);
            Assert.Equal(5, result.Longest);
        }

        [Fact]
        public void CalculateDailyStreak_WithGapBeforeToday_BreaksCurrentButKeepsLongest()
        {
            // Mirrors the exact scenario tested manually earlier:
            // Aug 24, 25, 26 (3-day run), gap on 27, then today (Aug 28)
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dates = new List<DateOnly>
            {
                today.AddDays(-4), // start of a 3-day run
                today.AddDays(-3),
                today.AddDays(-2),
                // gap: today.AddDays(-1) deliberately skipped
                today
            };

            var result = StreakCalculator.CalculateDailyStreak(dates);

            Assert.Equal(1, result.Current);   // only today counts
            Assert.Equal(3, result.Longest);   // the earlier 3-day run
        }

        [Fact]
        public void CalculateDailyStreak_WithNoRecentCheckIn_ReturnsZeroCurrentStreak()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dates = new List<DateOnly>
            {
                today.AddDays(-10),
                today.AddDays(-9),
                today.AddDays(-8)
                // nothing recent - streak should be considered broken
            };

            var result = StreakCalculator.CalculateDailyStreak(dates);

            Assert.Equal(0, result.Current);
            Assert.Equal(3, result.Longest); // longest run is still preserved
        }

        [Fact]
        public void CalculateDailyStreak_FillingAGap_MergesIntoOneContinuousStreak()
        {
            // Mirrors the retroactive check-in test done manually earlier
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dates = new List<DateOnly>
            {
                today.AddDays(-4),
                today.AddDays(-3),
                today.AddDays(-2),
                today.AddDays(-1), // the previously-missing day, now filled in
                today
            };

            var result = StreakCalculator.CalculateDailyStreak(dates);

            Assert.Equal(5, result.Current);
            Assert.Equal(5, result.Longest);
        }

        [Fact]
        public void CalculateDailyStreak_WithDuplicateDates_HandlesGracefully()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dates = new List<DateOnly> { today, today, today.AddDays(-1) };

            var result = StreakCalculator.CalculateDailyStreak(dates);

            // Duplicates shouldn't inflate the streak count
            Assert.Equal(2, result.Current);
            Assert.Equal(2, result.Longest);
        }

        [Theory]
        [InlineData(3, 3, true)]   // exactly meets target
        [InlineData(4, 3, true)]   // exceeds target
        [InlineData(2, 3, false)]  // falls short of target
        public void CalculateWeeklyTargetStreak_SingleWeek_MeetsOrMissesTarget(
            int checkInsThisWeek, int target, bool shouldQualify)
        {
            // Build check-ins spread across the current ISO week
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1); // rough Monday anchor

            var dates = new List<DateOnly>();
            for (int i = 0; i < checkInsThisWeek; i++)
            {
                dates.Add(startOfWeek.AddDays(i));
            }

            var result = StreakCalculator.CalculateWeeklyTargetStreak(dates, target);

            Assert.Equal(shouldQualify ? 1 : 0, result.Current);
        }

        [Fact]
        public void CalculateWeeklyTargetStreak_WithNoCheckIns_ReturnsZeroZero()
        {
            var result = StreakCalculator.CalculateWeeklyTargetStreak(new List<DateOnly>(), 3);

            Assert.Equal(0, result.Current);
            Assert.Equal(0, result.Longest);
        }
    }
}
