namespace NotificationService.DTOs
{
    public class MilestoneNotificationDto
    {
        public int UserId { get; set; }
        public int HabitId { get; set; }
        public string HabitName { get; set; } = string.Empty;
        public int Milestone { get; set; }
    }
}
