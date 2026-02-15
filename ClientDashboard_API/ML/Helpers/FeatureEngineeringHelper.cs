using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.ML.Helpers
{
    public static class FeatureEngineeringHelper
    {
        // Converts TrainerDailyRevenue records into ML training data
        // calculates the "NextMonthRevenue" label by looking ahead 30 days.

        public static List<TrainerRevenueData> PrepareTrainingData(List<TrainerDailyRevenue> dailyRecords)
        {
            var trainingData = new List<TrainerRevenueData>();

            // group by month to calculate next months revenue 
            var monthlyGroups = dailyRecords
                .GroupBy(r => new { r.AsOfDate.Year, r.AsOfDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .ToList();

            for(int i = 0; i < monthlyGroups.Count; i++)
            {
                var currentMonth = monthlyGroups[i];
                var nextMonth = i == monthlyGroups.Count - 1 ? monthlyGroups[i] : monthlyGroups[i + 1];

                // last day of the month used as a snapshot
                var lastDayOfMonth = currentMonth.OrderByDescending(r => r.AsOfDate).First();

                // Calculating next month's total revenue
                // in order to track next month revenue patterns
                var nextMonthRevenue = nextMonth.Sum(r => r.RevenueToday);

                var trainingExample = new TrainerRevenueData
                {
                    // raw features
                    ActiveClients = lastDayOfMonth.ActiveClients,
                    TotalSessionsThisMonth = lastDayOfMonth.TotalSessionsThisMonth,
                    AverageSessionPrice = (float)lastDayOfMonth.AverageSessionPrice,
                    NewClientsThisMonth = lastDayOfMonth.NewClientsThisMonth,
                    MonthlyRevenueThusFar = (float)lastDayOfMonth.MonthlyRevenueThusFar,

                    SessionsPerClient = lastDayOfMonth.ActiveClients > 0
                    ? (float)lastDayOfMonth.TotalSessionsThisMonth / lastDayOfMonth.ActiveClients
                    : 0,

                    DayOfMonth = lastDayOfMonth.AsOfDate.Day,
                    GrowthRate = lastDayOfMonth.ActiveClients > 0
                    ? (float)lastDayOfMonth.NewClientsThisMonth / lastDayOfMonth.ActiveClients
                    : 0,

                    // Label (target)
                    NextMonthRevenue = (float)nextMonthRevenue
                };

                trainingData.Add(trainingExample);
            }

            return trainingData;
        }

        // converts the current month's latest data into a predicition input
        public static TrainerRevenueData PreparePredictionData(TrainerDailyRevenue currentMonthSnapshot)
        {
            return new TrainerRevenueData
            {
                ActiveClients = currentMonthSnapshot.ActiveClients,
                TotalSessionsThisMonth = currentMonthSnapshot.TotalSessionsThisMonth,
                AverageSessionPrice = (float)currentMonthSnapshot.AverageSessionPrice,
                NewClientsThisMonth = currentMonthSnapshot.NewClientsThisMonth,
                MonthlyRevenueThusFar = (float)currentMonthSnapshot.MonthlyRevenueThusFar,

                SessionsPerClient = currentMonthSnapshot.ActiveClients > 0
                    ? (float)currentMonthSnapshot.TotalSessionsThisMonth / currentMonthSnapshot.ActiveClients
                    : 0,
                DayOfMonth = currentMonthSnapshot.AsOfDate.Day,
                GrowthRate = currentMonthSnapshot.ActiveClients > 0
                    ? (float)currentMonthSnapshot.NewClientsThisMonth / currentMonthSnapshot.ActiveClients
                    : 0,
            };
        }
    }

}
