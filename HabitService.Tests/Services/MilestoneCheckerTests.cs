using HabitService.Services;
using Xunit;

namespace HabitService.Tests.Services
{
    public class MilestoneCheckerTests
    {
        [Theory]
        [InlineData(6, 7, 7)]     // crosses the 7-day milestone exactly
        [InlineData(29, 30, 30)]  // crosses the 30-day milestone exactly
        [InlineData(99, 100, 100)] // crosses the 100-day milestone exactly
        public void GetCrossedMilestone_WhenStreakCrossesExactly_ReturnsThatMilestone(
            int previousStreak, int newStreak, int expectedMilestone)
        {
            var result = MilestoneChecker.GetCrossedMilestone(previousStreak, newStreak);

            Assert.Equal(expectedMilestone, result);
        }

        [Theory]
        [InlineData(5, 10, 7)]     // jumps straight past 7 (e.g. backdated check-ins)
        [InlineData(20, 35, 30)]  // jumps straight past 30
        public void GetCrossedMilestone_WhenStreakJumpsPastMilestone_StillDetectsIt(
            int previousStreak, int newStreak, int expectedMilestone)
        {
            var result = MilestoneChecker.GetCrossedMilestone(previousStreak, newStreak);

            Assert.Equal(expectedMilestone, result);
        }

        [Theory]
        [InlineData(3, 4)]    // ordinary increment, no milestone nearby
        [InlineData(10, 11)]  // between milestones
        [InlineData(50, 55)]  // between milestones
        public void GetCrossedMilestone_WhenNoMilestoneCrossed_ReturnsNull(
            int previousStreak, int newStreak)
        {
            var result = MilestoneChecker.GetCrossedMilestone(previousStreak, newStreak);

            Assert.Null(result);
        }

        [Fact]
        public void GetCrossedMilestone_WhenStreakDecreases_ReturnsNull()
        {
            // A streak going down (e.g. after a break resets the count)
            // should never trigger a milestone notification
            var result = MilestoneChecker.GetCrossedMilestone(previousStreak: 10, newStreak: 1);

            Assert.Null(result);
        }

        [Fact]
        public void GetCrossedMilestone_WhenAlreadyPastMilestone_DoesNotReTrigger()
        {
            // Streak was already at 8 (past the 7-day milestone), stays
            // in the same range - shouldn't fire again for 7
            var result = MilestoneChecker.GetCrossedMilestone(previousStreak: 8, newStreak: 9);

            Assert.Null(result);
        }

        [Fact]
        public void GetCrossedMilestone_WhenCrossingTwoMilestonesAtOnce_ReturnsTheFirstOne()
        {
            // A huge jump (e.g. importing old data) that crosses both 7 and 30
            // should return 7 - the Milestones array is checked in order,
            // and this documents that behavior explicitly
            var result = MilestoneChecker.GetCrossedMilestone(previousStreak: 5, newStreak: 40);

            Assert.Equal(7, result);
        }
    }
}