namespace HabitService.DTOs
{
    public class HabitInsightSummary
    {
        public string HabitName { get; set; } = string.Empty;
        public string FrequencyType { get; set; } = string.Empty;
        public int? TargetPerWeek { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalCheckIns { get; set; }
        public int CheckInsLast30Days { get; set; }
        public double CompletionRateLast30Days { get; set; }
        public List<string> MostMissedWeekDays { get; set; } = new List<string>();
    }
}
