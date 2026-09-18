namespace HabitService.DTOs
{
    public class StreakResponseDto
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}
