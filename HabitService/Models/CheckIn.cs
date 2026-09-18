namespace HabitService.Models
{
    public class CheckIn
    {
        public int Id { get; set; }
        public int HabitId { get; set; }
        public DateOnly Date { get; set; }
        public DateTime CreatedAt { get; set; }

        public Habit Habit { get; set; } = null!;
    }
}
