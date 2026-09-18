namespace HabitService.Services
{
    public static class MilestoneChecker
    {
        private static readonly int[] Milestones = { 7, 30, 100 };

        public static int? GetCrossedMilestone(int previousStreak, int newStreak)
        {
            foreach (var milestone in Milestones)
            {
                if (previousStreak < milestone && newStreak >= milestone)
                {
                    return milestone;
                }
            }
            return null;
        }
    }
}
