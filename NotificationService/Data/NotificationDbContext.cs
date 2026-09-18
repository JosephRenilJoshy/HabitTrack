using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
            : base(options) { }

        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(n => n.Type)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(n => n.CreatedAt)
                    .IsRequired();

                entity.HasIndex(n => n.UserId);
            });
        }
    }
}
