using HabitService.Data;
using HabitService.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitService.Repositories
{
    public class HabitRepository : IHabitRepository
    {
        private readonly HabitDbContext _context;

        public HabitRepository(HabitDbContext context)
        {
            _context = context;
        }

        public async Task<Habit?> GetByIdAsync(int id)
        {
            return await _context.Habits.FindAsync(id);
        }

        public async Task<Habit?> GetByIdWithCheckInsAsync(int id)
        {
            // Eager loading via Include — fetches the Habit AND its related
            // CheckIns in a single query (a JOIN under the hood), avoiding
            // the N+1 problem we'll demonstrate deliberately later
            return await _context.Habits
                .Include(h => h.CheckIns)
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task<List<Habit>> GetByUserIdAsync(int userId)
        {
            // AsNoTracking: this is a read-only query — we're never going to
            // modify these entities in this method, so we tell EF Core not
            // to bother tracking them for changes. Reduces overhead.
            return await _context.Habits
                .AsNoTracking()
                .Where(h => h.UserId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(Habit habit)
        {
            await _context.Habits.AddAsync(habit);
        }

        public Task UpdateAsync(Habit habit)
        {
            // This handles the DISCONNECTED entity scenario: 'habit' here
            // did NOT come from a query in this same DbContext instance —
            // it's a fresh object (e.g., built from a DTO), so EF Core's
            // change tracker has never seen it. We must explicitly tell
            // EF Core "this entity is Modified" so SaveChanges knows to
            // generate an UPDATE statement for it.
            _context.Entry(habit).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Habit habit)
        {
            _context.Habits.Remove(habit);
            return Task.CompletedTask;
        }

        public async Task<bool> HasCheckInForDateAsync(int habitId, DateOnly date)
        {
            return await _context.CheckIns
                .AnyAsync(c => c.HabitId == habitId && c.Date == date);
        }

        public async Task AddCheckInAsync(CheckIn checkIn)
        {
            await _context.CheckIns.AddAsync(checkIn);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<DateOnly>> GetCheckInDatesAsync(int habitId)
        {
            return await _context.CheckIns
                .AsNoTracking()
                .Where(c => c.HabitId == habitId)
                .OrderBy(c => c.Date)
                .Select(c => c.Date)
                .ToListAsync();
        }

        public async Task<List<Habit>> GetAllByUserIdAsync(int userId)
        {
            return await _context.Habits
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<Habit>> GetAllHabitsAsync()
        {
            return await _context.Habits
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<(Habit habit, int CheckInCount)>> GetAllByUserIdWithCountsAsync(int userId)
        {
            // FIX: instead of N+1 separate queries, this issues a SINGLE query
            // that joins Habits to CheckIns and counts them server-side, in SQL —
            // no per-habit round-trips at all.
            var result = await _context.Habits
                            .AsNoTracking()
                            .Where(h => h.UserId == userId)
                            .Select(h => new
                            {
                                Habit = h,
                                CheckInCount = h.CheckIns.Count
                            })
                            .ToListAsync();

            return result.Select(r => (r.Habit, r.CheckInCount)).ToList();
        }
    }
}
