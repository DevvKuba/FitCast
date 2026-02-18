using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;

namespace ClientDashboard_API.ML.Helpers
{
    public static class DummyDataGenerator
    {
        /// <summary>
        /// Generates realistic TrainerDailyRevenue records for ML testing.
        /// Creates data with realistic patterns: growth trends, seasonality, randomness.
        /// </summary>
        /// <param name="trainerId">Trainer to generate data for</param>
        /// <param name="numberOfMonths">How many months of data (default: 6 months = 180 days)</param>
        /// <param name="startDate">Starting date (default: 6 months ago)</param>
        public static List<TrainerDailyRevenue> GenerateRealisticRevenueData(int trainerId, int numberOfMonths = 6, DateOnly? startDate = null)
        {
            var records = new List<TrainerDailyRevenue>();
            var random = new Random(trainerId); // Seed with trainerId for reproducibility
            
            // Start from X months ago if not specified
            var currentDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-numberOfMonths));
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // === BASE PARAMETERS (realistic starting values) ===
            int baseActiveClients = 12;  // Starting client base
            decimal baseSessionPrice = 40.0m;  // Average session price
            int baseSessionsPerMonth = 30;  // Total monthly sessions

            // === GROWTH TRENDS ===
            double sessionGrowthRate = 0;
            
            int currentMonth = currentDate.Month;
            int monthCounter = 0;
            
            while (currentDate <= endDate)
            {
                // === DETECT MONTH CHANGE (apply growth) ===
                if (currentDate.Month != currentMonth)
                {
                    currentMonth = currentDate.Month;
                    monthCounter++;
                    
                    // Apply monthly growth with some randomness
                    baseActiveClients += random.Next(0, 3); // Add 0-2 new clients per month
                    baseActiveClients -= random.Next(0, 1); // Remove 0-1 new clients per month
                    sessionGrowthRate = random.NextDouble();

                    var growthIndicator = random.Next(0, 2);
                    if(growthIndicator == 0)
                    {
                        sessionGrowthRate= sessionGrowthRate * 1;
                    }

                    baseSessionsPerMonth = (int)(baseSessionsPerMonth * (1 + sessionGrowthRate));

                }
                
                // === DAILY CALCULATIONS ===
                
                // Sessions per day (varies throughout month)
                int dayOfMonth = currentDate.Day;
                double monthProgressFactor = (double)dayOfMonth / DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                
                // More sessions toward month-end (common pattern in fitness)
                double endOfMonthBoost = monthProgressFactor > 0.8 ? 1.3 : 1.0;
                double dailySessions = (int)(baseSessionsPerMonth / 30.0 * endOfMonthBoost);
                dailySessions = Math.Max(0, dailySessions); // Can have 0 sessions on some days
                
                // New clients this month (cumulative through the month)
                int newClientsThisMonth = monthCounter > 0 
                    ? random.Next(1, 3)  // 1-2 new clients per month
                    : 0;
                
                // Revenue today (sessions * price with some variance)
                decimal revenueToday = (int)dailySessions * baseSessionPrice;
                
                // Monthly revenue so far (sum of all days in current month)
                var monthStartDate = new DateOnly(currentDate.Year, currentDate.Month, 1);
                decimal monthlyRevenueThusFar = records
                    .Where(r => r.TrainerId == trainerId && r.AsOfDate >= monthStartDate && r.AsOfDate < currentDate)
                    .Sum(r => r.RevenueToday) + revenueToday;
                
                // Total sessions this month (cumulative)
                int totalSessionsThisMonth = records
                    .Where(r => r.TrainerId == trainerId && r.AsOfDate >= monthStartDate && r.AsOfDate < currentDate)
                    .Sum(r => r.RevenueToday > 0 ? (int)(r.RevenueToday / r.AverageSessionPrice) : 0) + (int)dailySessions;
                
                // Create the daily record
                var record = new TrainerDailyRevenue
                {
                    TrainerId = trainerId,
                    AsOfDate = currentDate,
                    RevenueToday = Math.Round(revenueToday, 2),
                    MonthlyRevenueThusFar = Math.Round(monthlyRevenueThusFar, 2),
                    TotalSessionsThisMonth = totalSessionsThisMonth,
                    NewClientsThisMonth = newClientsThisMonth,
                    ActiveClients = baseActiveClients,
                    AverageSessionPrice = Math.Round(baseSessionPrice, 2)
                };
                
                records.Add(record);
                
                // Move to next day
                currentDate = currentDate.AddDays(1);

            }
            
            return records;
        }
        
    }
}
