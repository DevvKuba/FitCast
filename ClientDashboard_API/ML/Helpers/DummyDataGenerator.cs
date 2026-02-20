using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.ML.Models;

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
        public static List<TrainerDailyRevenue> GenerateRealisticRevenueData(int trainerId, int numberOfMonths)
        {
            var records = new List<TrainerDailyRevenue>();
            var random = new Random(trainerId); // Seed with trainerId for reproducibility
            
            // Start from X months ago if not specified
            var currentDate =DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-numberOfMonths));
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // === BASE PARAMETERS (realistic starting values) ===
            int baseActiveClients = 12;  // Starting client base
            decimal baseSessionPrice = 40.0m;  // Average session price
            int baseSessionsPerMonth = 30;  // Total monthly sessions

            // === GROWTH TRENDS ===
            double sessionGrowthRate = 0;
            
            int currentMonth = currentDate.Month;
            int monthCounter = 0;
            int newClientsThisMonth = 0;


            while (currentDate <= endDate)
            {
                // === DETECT MONTH CHANGE (apply growth) ===
                if (currentDate.Month != currentMonth)
                {
                    currentMonth = currentDate.Month;
                    monthCounter++;

                    newClientsThisMonth = monthCounter > 0
                    ? random.Next(1, 3)  // 1-2 new clients per month
                    : 0;

                    // Apply monthly growth with some randomness
                    baseActiveClients += random.Next(0, 3); // Add 0-2 new clients per month
                    baseActiveClients -= random.Next(0, 1); // Remove 0-1 new clients per month

                    // redeclared each month
                    sessionGrowthRate = random.NextDouble();

                    var growthIndicator = random.Next(0, 4);
                    // minor chance of sessionsGrowthRate decrease
                    if(growthIndicator == 0)
                    {
                        sessionGrowthRate = -(0.03 + random.NextDouble() * 0.05); // -0.03 - -0.08
                    }

                    baseSessionsPerMonth = (int)(baseSessionsPerMonth * (1 + sessionGrowthRate));

                }

                // === DAILY CALCULATIONS ===

                // 1. Weekly pattern (trainers have busy/rest days)
                var dayOfWeek = currentDate.DayOfWeek;
                double weeklyMultiplier = dayOfWeek switch
                {
                    DayOfWeek.Sunday => 0.4,       // Light sessions or rest
                    DayOfWeek.Monday => 1.5,       // Busy start of week
                    DayOfWeek.Tuesday => 1.3,      // Still busy
                    DayOfWeek.Wednesday => 1.2,    // Mid-week
                    DayOfWeek.Thursday => 1.4,     // Pre-weekend push
                    DayOfWeek.Friday => 1.0,       // Lighter day
                    DayOfWeek.Saturday => 0.4,     // Light sessions or rest
                    _ => 1.0
                };

                double workingDaysPerMonth = 22.0;
                double targetSessionsPerWorkingDay = baseSessionsPerMonth / workingDaysPerMonth;

                // Sessions per day (varies throughout month)
                int dayOfMonth = currentDate.Day;
                int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                double monthProgressFactor = (double)dayOfMonth / daysInMonth;
                
                // More sessions toward month-end (common pattern in fitness)
                double endOfMonthBoost = monthProgressFactor > 0.8 ? 1.3 : 1.0;

                double dailyVariance = 0.7 + (random.NextDouble() * 0.6);

                // accounts many daily/weekly/monthly factors in determination
                double dailySessions = targetSessionsPerWorkingDay
                    * endOfMonthBoost
                    * dailyVariance
                    * weeklyMultiplier;

                dailySessions = Math.Max(0, Math.Round(dailySessions, 0));
                
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

        public static List<TrainerDailyRevenue> GenerateExtendedRevenueData(TrainerStatistics trainerStatistics, int trainerId, int numberOfMonths)
        {
            var records = new List<TrainerDailyRevenue>();
            var random = new Random(trainerId); 

            // Start from X months ago if not specified
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-numberOfMonths));
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

            // === BASE PARAMETERS (realistic starting values) ===
            int baseActiveClients = trainerStatistics.BaseActiveClients;  
            decimal baseSessionPrice = trainerStatistics.BaseSessionsPrice; 
            int baseSessionsPerMonth = trainerStatistics.BaseSessionsPerMonth;  

            // === GROWTH TRENDS ===
            double sessionGrowthRate = trainerStatistics.SessionMonthlyGrowth;

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

                    var growthIndicator = random.Next(0, 3);
                    // minor chance of sessionsGrowthRate decrease
                    if (growthIndicator == 0)
                    {
                        sessionGrowthRate = sessionGrowthRate * -1;
                    }

                    baseSessionsPerMonth = (int)(baseSessionsPerMonth * (1 + sessionGrowthRate));

                }

                // === DAILY CALCULATIONS ===

                int dayOfMonth = currentDate.Day;
                double monthProgressFactor = (double)dayOfMonth / DateTime.DaysInMonth(currentDate.Year, currentDate.Month);

                // More sessions toward month-end (common pattern in fitness)
                double endOfMonthBoost = monthProgressFactor > 0.8 ? 1.3 : 1.0;
                double dailySessions = (baseSessionsPerMonth / 30.0 * endOfMonthBoost);
                //dailySessions = Math.Max(0, dailySessions); // Can have 0 sessions on some days
                dailySessions = Math.Round(dailySessions, 0);


                // New clients this month (cumulative through the month)
                int newClientsThisMonth = monthCounter > 0
                    ? random.Next(1, 3)  // 1-2 new clients per month
                    : 0;

                decimal revenueToday = (int)dailySessions * baseSessionPrice;

                var monthStartDate = new DateOnly(currentDate.Year, currentDate.Month, 1);
                decimal monthlyRevenueThusFar = records
                    .Where(r => r.TrainerId == trainerId && r.AsOfDate >= monthStartDate && r.AsOfDate < currentDate)
                    .Sum(r => r.RevenueToday) + revenueToday;

                int totalSessionsThisMonth = records
                    .Where(r => r.TrainerId == trainerId && r.AsOfDate >= monthStartDate && r.AsOfDate < currentDate)
                    .Sum(r => r.RevenueToday > 0 ? (int)(r.RevenueToday / r.AverageSessionPrice) : 0) + (int)dailySessions;

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
