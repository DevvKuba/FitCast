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
        public static List<TrainerDailyRevenue> GenerateRealisticRevenueData(
            int trainerId,
            int numberOfMonths = 6,
            DateOnly? startDate = null)
        {
            var records = new List<TrainerDailyRevenue>();
            var random = new Random(trainerId); // Seed with trainerId for reproducibility
            
            // Start from X months ago if not specified
            var currentDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-numberOfMonths));
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // === BASE PARAMETERS (realistic starting values) ===
            int baseActiveClients = 8;  // Starting client base
            decimal baseSessionPrice = 40.0m;  // Average session price
            int baseSessionsPerMonth = 24;  // Total monthly sessions
            
            // === GROWTH TRENDS ===
            double clientGrowthRate = 0.08;  // 8% monthly client growth
            double priceGrowthRate = 0.02;   // 2% monthly price growth
            double sessionGrowthRate = 0.05; // 5% monthly session growth
            
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
                    baseSessionPrice *= (decimal)(1 + priceGrowthRate + random.NextDouble() * 0.01);
                    baseSessionsPerMonth = (int)(baseSessionsPerMonth * (1 + sessionGrowthRate));
                }
                
                // === DAILY CALCULATIONS ===
                
                // Active clients (minor daily fluctuations)
                int dailyActiveClients = baseActiveClients + random.Next(-1, 2);
                dailyActiveClients = Math.Max(1, dailyActiveClients); // Never go below 1
                
                // Sessions per day (varies throughout month)
                int dayOfMonth = currentDate.Day;
                double monthProgressFactor = (double)dayOfMonth / DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                
                // More sessions toward month-end (common pattern in fitness)
                double endOfMonthBoost = monthProgressFactor > 0.8 ? 1.3 : 1.0;
                int dailySessions = (int)(baseSessionsPerMonth / 30.0 * endOfMonthBoost * (0.7 + random.NextDouble() * 0.6));
                dailySessions = Math.Max(0, dailySessions); // Can have 0 sessions on some days
                
                // New clients this month (cumulative through the month)
                int newClientsThisMonth = monthCounter > 0 
                    ? random.Next(1, 4)  // 1-3 new clients per month
                    : 0;
                
                // Revenue today (sessions * price with some variance)
                decimal sessionPriceToday = baseSessionPrice * (decimal)(0.95 + random.NextDouble() * 0.1);
                decimal revenueToday = dailySessions * sessionPriceToday;
                
                // Monthly revenue so far (sum of all days in current month)
                var monthStartDate = new DateOnly(currentDate.Year, currentDate.Month, 1);
                decimal monthlyRevenueThusFar = records
                    .Where(r => r.TrainerId == trainerId && r.AsOfDate >= monthStartDate && r.AsOfDate < currentDate)
                    .Sum(r => r.RevenueToday) + revenueToday;
                
                // Total sessions this month (cumulative)
                int totalSessionsThisMonth = records
                    .Where(r => r.TrainerId == trainerId && r.AsOfDate >= monthStartDate && r.AsOfDate < currentDate)
                    .Sum(r => r.RevenueToday > 0 ? (int)(r.RevenueToday / r.AverageSessionPrice) : 0) + dailySessions;
                
                // Create the daily record
                var record = new TrainerDailyRevenue
                {
                    TrainerId = trainerId,
                    AsOfDate = currentDate,
                    RevenueToday = Math.Round(revenueToday, 2),
                    MonthlyRevenueThusFar = Math.Round(monthlyRevenueThusFar, 2),
                    TotalSessionsThisMonth = totalSessionsThisMonth,
                    NewClientsThisMonth = newClientsThisMonth,
                    ActiveClients = dailyActiveClients,
                    AverageSessionPrice = Math.Round(sessionPriceToday, 2)
                };
                
                records.Add(record);
                
                // Move to next day
                currentDate = currentDate.AddDays(1);

            }
            
            return records;
        }
        
        /// <summary>
        /// Generates more aggressive growth pattern (for testing high R² scenarios)
        /// </summary>
        public static List<TrainerDailyRevenue> GenerateHighGrowthScenario(int trainerId, int numberOfMonths = 6)
        {
            var records = new List<TrainerDailyRevenue>();
            var random = new Random(trainerId + 100);
            
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-numberOfMonths));
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // High growth: doubles clients every 3 months
            int baseActiveClients = 5;
            decimal baseSessionPrice = 70.0m;
            int baseSessionsPerMonth = 20;
            
            int currentMonth = currentDate.Month;
            int monthCounter = 0;
            
            while (currentDate <= endDate)
            {
                if (currentDate.Month != currentMonth)
                {
                    currentMonth = currentDate.Month;
                    monthCounter++;
                    
                    // Aggressive growth
                    baseActiveClients += 2 + monthCounter; // Accelerating client growth
                    baseSessionsPerMonth += 8;
                    baseSessionPrice *= 1.03m; // 3% monthly price increase
                }
                
                int dailyActiveClients = baseActiveClients + random.Next(-1, 3);
                int dailySessions = (int)(baseSessionsPerMonth / 30.0 * (0.8 + random.NextDouble() * 0.4));
                decimal revenueToday = dailySessions * baseSessionPrice;
                
                var monthStartDate = new DateOnly(currentDate.Year, currentDate.Month, 1);
                decimal monthlyRevenueThusFar = records
                    .Where(r => r.TrainerId == trainerId && r.AsOfDate >= monthStartDate && r.AsOfDate < currentDate)
                    .Sum(r => r.RevenueToday) + revenueToday;
                
                int totalSessionsThisMonth = records
                    .Where(r => r.TrainerId == trainerId && r.AsOfDate >= monthStartDate && r.AsOfDate < currentDate)
                    .Count(r => r.RevenueToday > 0) + dailySessions;
                
                records.Add(new TrainerDailyRevenue
                {
                    TrainerId = trainerId,
                    AsOfDate = currentDate,
                    RevenueToday = Math.Round(revenueToday, 2),
                    MonthlyRevenueThusFar = Math.Round(monthlyRevenueThusFar, 2),
                    TotalSessionsThisMonth = totalSessionsThisMonth,
                    NewClientsThisMonth = monthCounter > 0 ? random.Next(2, 5) : 0,
                    ActiveClients = dailyActiveClients,
                    AverageSessionPrice = Math.Round(baseSessionPrice, 2)
                });
                
                currentDate = currentDate.AddDays(1);
            }
            
            return records;
        }
        
        /// <summary>
        /// Generates flat/declining pattern (for testing poor model scenarios)
        /// </summary>
        public static List<TrainerDailyRevenue> GenerateFlatScenario(int trainerId, int numberOfMonths = 6)
        {
            var records = new List<TrainerDailyRevenue>();
            var random = new Random(trainerId + 200);
            
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-numberOfMonths));
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // Stagnant business
            int activeClients = 10;
            decimal sessionPrice = 60.0m;
            int sessionsPerMonth = 40;
            
            while (currentDate <= endDate)
            {
                // Very little variation
                int dailySessions = (int)(sessionsPerMonth / 30.0 * (0.9 + random.NextDouble() * 0.2));
                decimal revenueToday = dailySessions * sessionPrice * (decimal)(0.95 + random.NextDouble() * 0.1);
                
                var monthStartDate = new DateOnly(currentDate.Year, currentDate.Month, 1);
                decimal monthlyRevenueThusFar = records
                    .Where(r => r.TrainerId == trainerId && r.AsOfDate >= monthStartDate && r.AsOfDate < currentDate)
                    .Sum(r => r.RevenueToday) + revenueToday;
                
                records.Add(new TrainerDailyRevenue
                {
                    TrainerId = trainerId,
                    AsOfDate = currentDate,
                    RevenueToday = Math.Round(revenueToday, 2),
                    MonthlyRevenueThusFar = Math.Round(monthlyRevenueThusFar, 2),
                    TotalSessionsThisMonth = (int)(monthlyRevenueThusFar / sessionPrice),
                    NewClientsThisMonth = random.Next(0, 2), // Minimal growth
                    ActiveClients = activeClients + random.Next(-2, 2),
                    AverageSessionPrice = Math.Round(sessionPrice, 2)
                });
                
                currentDate = currentDate.AddDays(1);
            }
            
            return records;
        }
    }
}
