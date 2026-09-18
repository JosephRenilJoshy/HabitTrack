using HabitService.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HabitService.Filters
{
    public class StreakCacheResourceFilter : IResourceFilter
    {
        private readonly IStreakCacheService _cache;

        public StreakCacheResourceFilter(IStreakCacheService cache)
        {
            _cache = cache;
        }
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // Nothing needed after — caching the result on the way OUT
            // is handled inside the service layer instead
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context.RouteData.Values["id"] is not string idStr ||
                !int.TryParse(idStr, out int habitId))
            {
                return; // can't determine habit id
            }

            if ((_cache.TryGet(habitId, out var cached)))
            {
                // SHORT-CIRCUIT: setting context.Result here means the action
                // method (GetStreak) is NEVER CALLED AT ALL — no model binding,
                // no DbContext query, no streak recalculation. This is exactly
                // what distinguishes a Resource Filter from an Action Filter.
                context.Result = new Microsoft.AspNetCore.Mvc.OkObjectResult(cached);
            }
        }
    }
}
