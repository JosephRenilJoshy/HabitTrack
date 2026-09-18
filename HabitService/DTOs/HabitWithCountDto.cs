namespace HabitService.DTOs
{
    public class HabitWithCountDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CheckInCount { get; set; }
    }
}
