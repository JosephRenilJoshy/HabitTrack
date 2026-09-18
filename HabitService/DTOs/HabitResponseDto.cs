namespace HabitService.DTOs
{
    public class HabitResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FrequencyType { get; set; } = string.Empty;
        public int? TargetPerWeek { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
