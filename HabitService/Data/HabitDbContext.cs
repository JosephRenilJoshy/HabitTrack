using HabitService.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitService.Data
{
    public class HabitDbContext : DbContext
    {
        public HabitDbContext(DbContextOptions<HabitDbContext> options) : base(options)
        {
        }

        public DbSet<Habit> Habits { get; set; }
        public DbSet<CheckIn> CheckIns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Habit>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.Property(h => h.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(h => h.UserId)
                    .IsRequired();

                entity.Property(h => h.FrequencyType)
                    .IsRequired()
                    .HasConversion<string>();  // store enum as readable text, not an int, in the DB

                entity.Property(h => h.CreatedAt)
                    .IsRequired();

                // One-to-many: one Habit has many CheckIns
                entity.HasMany(h => h.CheckIns)
                    .WithOne(c => c.Habit)
                    .HasForeignKey(c => c.HabitId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CheckIn>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Date)
                    .IsRequired();

                entity.Property(c => c.CreatedAt)
                    .IsRequired();

                // Prevent duplicate check-ins for the same habit on the same date
                entity.HasIndex(c => new { c.HabitId, c.Date })
                    .IsUnique();
            });
        }
    }
}
