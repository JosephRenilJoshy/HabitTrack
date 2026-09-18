namespace HabitService.Models
{
    public enum FrequencyType
    {
        Daily,
        WeeklyTarget
    }
    public class Habit
    {

        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public FrequencyType FrequencyType { get; set; }
        public int? TargetPerWeek { get; set; } // Only applicable for WeeklyTarget frequency
        public DateTime CreatedAt { get; set; } 

        public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    }
}
