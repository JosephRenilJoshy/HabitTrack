namespace NotificationService.Models
{
    public enum NotificationType
    {
        AtRisk,
        Milestones
    }
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int HabitId { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } 
        public DateTime CreatedAt { get; set; }
    }
}
