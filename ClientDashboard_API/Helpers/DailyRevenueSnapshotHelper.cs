using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.Helpers
{
    public static class DailyRevenueSnapshotHelper
    {
        // Rebuilds the workout-based figures of existing snapshots. Each day keeps the session price and
        // active client count it was recorded with, so a later price change doesn't reprice earlier days.
        // The workouts must cover each snapshot's month from its 1st, for the month-to-date totals.
        public static void RefreshFromWorkouts(List<TrainerDailyRevenue> snapshots, List<Workout> workouts)
        {
            foreach (var snapshot in snapshots)
            {
                var dayWorkouts = workouts.Where(w => w.SessionDate == snapshot.AsOfDate).ToList();

                snapshot.SessionsToday = dayWorkouts.Count;
                snapshot.RevenueToday = dayWorkouts.Count * snapshot.AverageSessionPrice;
                snapshot.TotalSessionDuration = dayWorkouts.Sum(w => w.Duration);

                var monthWorkoutsSoFar = workouts
                    .Where(w => w.SessionDate.Year == snapshot.AsOfDate.Year
                        && w.SessionDate.Month == snapshot.AsOfDate.Month
                        && w.SessionDate <= snapshot.AsOfDate)
                    .ToList();

                snapshot.TotalSessionsThisMonth = monthWorkoutsSoFar.Count;
                snapshot.MonthlyRevenueThusFar = monthWorkoutsSoFar.Sum(w => GetPriceOnDay(w.SessionDate, snapshots, snapshot.AverageSessionPrice));
            }
        }

        // Days without a snapshot have no recorded price, so they take the price of the snapshot being refreshed
        private static decimal GetPriceOnDay(DateOnly day, List<TrainerDailyRevenue> snapshots, decimal fallbackPrice)
        {
            var daySnapshot = snapshots.FirstOrDefault(s => s.AsOfDate == day);
            return daySnapshot?.AverageSessionPrice ?? fallbackPrice;
        }
    }
}
