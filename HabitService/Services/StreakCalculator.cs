namespace HabitService.Services
{
    public static class StreakCalculator
    {
        public static (int Current, int Longest) CalculateDailyStreak(List<DateOnly> checkInDates)
        {
            if(checkInDates.Count == 0)
            {
                return (0, 0);
            }

            var sortedDates = checkInDates.Distinct().OrderBy(d => d).ToList();

            int longest = 1;
            int running = 1;

            for (int i = 1; i < sortedDates.Count; i++)
            {
                // DayNumber = days since a fixed reference date (DateOnly.MinValue).
                // DateOnly doesn't support direct subtraction, so we convert to this
                // integer representation first — this also avoids manual edge-case
                // handling around month/year boundaries (e.g. Aug 31 -> Sep 1).
                var gap = sortedDates[i].DayNumber - sortedDates[i - 1].DayNumber;

                if (gap == 1)
                {
                    running++;
                }
                else if (gap > 1)
                {
                    longest = Math.Max(longest, running);
                    running = 1;
                }
            }

            longest = Math.Max(longest, running);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var mostRecent = sortedDates[^1]; // ^1 = last element (C# index-from-end syntax) ^2 = "second to last"
            var daysSinceLastCheckIn = today.DayNumber - mostRecent.DayNumber;

            int current = daysSinceLastCheckIn <= 1 ? running : 0;

            return (current, longest);
            
        }

        public static (int Current, int Longest) CalculateWeeklyTargetStreak(List<DateOnly> checkInDates, int targetPerWeek)
        {
            if(checkInDates.Count == 0)
            {
                return (0,0);
            }

            var weekGroups = checkInDates
                .GroupBy(GetIsoWeekKey)
                .OrderBy(g  => g.Key)
                .ToList();

            var qualifyingWeeks = weekGroups
                .Select(g => new {Week = g.Key, Met = g.Count() >=  targetPerWeek })
                .ToList();

            int longest = 0;
            int running = 0;

            foreach(var week in qualifyingWeeks)
            {
                if(week.Met)
                {
                    running++;
                    longest = Math.Max(longest, running);
                }
                else
                {
                    running = 0;
                }
            }

            var currentWeekKey = GetIsoWeekKey(DateOnly.FromDateTime(DateTime.Now));
            var lastWeek = qualifyingWeeks[^1];

            int current = (lastWeek.Week == currentWeekKey && lastWeek.Met) ||
                (weekGroups.Count >= 2 && qualifyingWeeks[^1].Met) ? running : 0;

            return (current, longest);
        }

        private static (int Year, int Week) GetIsoWeekKey(DateOnly date)
        {
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            var week = System.Globalization.ISOWeek.GetWeekOfYear(dateTime);
            var year = System.Globalization.ISOWeek.GetYear(dateTime);
            return (year, week);
        } 
    }
}
