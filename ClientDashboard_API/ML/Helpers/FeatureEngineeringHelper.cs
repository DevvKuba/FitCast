using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.ML.Helpers
{
    public static class FeatureEngineeringHelper
    {
        // Converts TrainerDailyRevenue records into ML training data
        // Creates multiple training examples per month (every 3 days)

        public static List<TrainerRevenueData> PrepareTrainingData(List<TrainerDailyRevenue> dailyRecords)
        {
            var trainingData = new List<TrainerRevenueData>();

            // Group by month to calculate next month's revenue 
            var monthlyGroups = dailyRecords
                .GroupBy(r => new { r.AsOfDate.Year, r.AsOfDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .ToList();

            // Preparing all but the latest month - since each current needs a corresponding next 
            for(int i = 0; i < monthlyGroups.Count - 1; i++)
            {
                var currentMonthRecords = monthlyGroups[i].OrderBy(r => r.AsOfDate).ToList();
                var nextMonth = monthlyGroups[i + 1];

                // Calculate next month's total revenue once (same for all snapshots)
                var nextMonthRevenue = nextMonth.Sum(r => r.RevenueToday);

                int daysInMonth = currentMonthRecords.Count;

                // Create snapshots every 3 days 
                // Example: For 30-day month, creates snapshots on days 2, 5, 8, 11, 14, 17, 20, 23, 26, 29
                for (int dayIndex = 2; dayIndex < daysInMonth; dayIndex += 3)
                {
                    var snapshot = currentMonthRecords[dayIndex];

                    int daysInCurrentMonth = DateTime.DaysInMonth(snapshot.AsOfDate.Year, snapshot.AsOfDate.Month);
                    int currentDay = snapshot.AsOfDate.Day;

                    int daysRemainingInMonth = daysInCurrentMonth - currentDay;


                    float revenueVelocity = currentDay > 0 && snapshot.MonthlyRevenueThusFar > 0
                        ? (float)snapshot.MonthlyRevenueThusFar / currentDay
                        : 0;

                    var trainingExample = new TrainerRevenueData
                    {
                        ActiveClients = snapshot.ActiveClients,
                        TotalSessionsThisMonth = snapshot.TotalSessionsThisMonth,
                        AverageSessionPrice = (float)snapshot.AverageSessionPrice,
                        NewClientsThisMonth = snapshot.NewClientsThisMonth,
                        MonthlyRevenueThusFar = (float)snapshot.MonthlyRevenueThusFar,

                        // Engineered features
                        SessionsPerClient = snapshot.ActiveClients > 0
                            ? (float)snapshot.TotalSessionsThisMonth / snapshot.ActiveClients
                            : 0,

                        DaysRemainingInMonth = daysRemainingInMonth,

                        GrowthRate = snapshot.ActiveClients > 0
                            ? (float)snapshot.NewClientsThisMonth / snapshot.ActiveClients
                            : 0,

                        RevenueVelocity = revenueVelocity,

                        // Label (target) - same for all snapshots in this month
                        NextMonthRevenue = (float)nextMonthRevenue
                    };

                    trainingData.Add(trainingExample);
                }
            }

            return trainingData;
        }

        // Converts the current month's latest data into a prediction input
        public static TrainerRevenueData PreparePredictionData(TrainerDailyRevenue currentMonthSnapshot)
        {
            // Calculate days remaining in the current month
            int daysInMonth = DateTime.DaysInMonth(currentMonthSnapshot.AsOfDate.Year, currentMonthSnapshot.AsOfDate.Month);
            int currentDay = currentMonthSnapshot.AsOfDate.Day;
            int daysRemainingInMonth = daysInMonth - currentDay;

            // Calculate revenue velocity (revenue earned per day so far)
            float revenueVelocity = currentDay > 0 && currentMonthSnapshot.MonthlyRevenueThusFar > 0
                ? (float)currentMonthSnapshot.MonthlyRevenueThusFar / currentDay
                : 0;

            return new TrainerRevenueData
            {
                ActiveClients = currentMonthSnapshot.ActiveClients,
                TotalSessionsThisMonth = currentMonthSnapshot.TotalSessionsThisMonth,
                AverageSessionPrice = (float)currentMonthSnapshot.AverageSessionPrice,
                NewClientsThisMonth = currentMonthSnapshot.NewClientsThisMonth,
                MonthlyRevenueThusFar = (float)currentMonthSnapshot.MonthlyRevenueThusFar,

                // Engineered features
                SessionsPerClient = currentMonthSnapshot.ActiveClients > 0
                    ? (float)currentMonthSnapshot.TotalSessionsThisMonth / currentMonthSnapshot.ActiveClients
                    : 0,

                DaysRemainingInMonth = daysRemainingInMonth,

                GrowthRate = currentMonthSnapshot.ActiveClients > 0
                    ? (float)currentMonthSnapshot.NewClientsThisMonth / currentMonthSnapshot.ActiveClients
                    : 0,

                RevenueVelocity = revenueVelocity
            };
        }
    }

}
