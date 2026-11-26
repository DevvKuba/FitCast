using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.Helpers
{
    // Builds the current month's daily records live from workouts, so the current month view reflects
    // workouts added or edited since the nightly snapshot. The records are only read, never saved.
    public static class CurrentMonthRecordsHelper
    {
        public static List<TrainerDailyRevenue> BuildLiveRecords(int trainerId, DateOnly firstDay, DateOnly today, List<Workout> monthWorkouts,
            List<TrainerDailyRevenue> monthSnapshots, int currentActiveClients, decimal sessionPrice)
        {
            var records = new List<TrainerDailyRevenue>();

            for (var day = firstDay; day <= today; day = day.AddDays(1))
            {
                var dayWorkouts = monthWorkouts.Where(w => w.SessionDate == day).ToList();

                records.Add(new TrainerDailyRevenue
                {
                    TrainerId = trainerId,
                    AsOfDate = day,
                    SessionsToday = dayWorkouts.Count,
                    RevenueToday = dayWorkouts.Count * sessionPrice,
                    TotalSessionDuration = dayWorkouts.Sum(w => w.Duration),
                    ActiveClients = GetActiveClientsOnDay(day, monthSnapshots, currentActiveClients),
                    AverageSessionPrice = sessionPrice
                });
            }

            return records;
        }

        // Past active client counts can't be rebuilt from workouts, so recorded days keep their snapshot's count
        private static int GetActiveClientsOnDay(DateOnly day, List<TrainerDailyRevenue> monthSnapshots, int currentActiveClients)
        {
            var snapshot = monthSnapshots.FirstOrDefault(s => s.AsOfDate == day);
            return snapshot?.ActiveClients ?? currentActiveClients;
        }
    }
}
