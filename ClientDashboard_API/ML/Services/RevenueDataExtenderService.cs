using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Helpers;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.ML.Services
{
    public class RevenueDataExtenderService(IUnitOfWork unitOfWork) : IRevenueDataExtenderService
    {
        public async Task<TrainerDailyRevenue> ProvideExtensionRecordsForRevenueDataAsync(int trainerId)
        {
            var firstRevenueRecord = await unitOfWork.TrainerDailyRevenueRepository.GetFirstRevenueRecordForTrainerAsync(trainerId);
            var allRevenueRecords = await unitOfWork.TrainerDailyRevenueRepository.GetAllRevenueRecordsForTrainerAsync(trainerId);

            var firstNewMonthsRevenueRecords = await unitOfWork.TrainerDailyRevenueRepository.GetFirstFullMonthOfRevenueRecordsAsync(allRevenueRecords);

            // a month from the first recorded trainer daily revenue record
            var monthlyRecords = await unitOfWork.TrainerDailyRevenueRepository.GetLastMonthsDayRecordsBasedOnFirstRecordAsync(firstRevenueRecord!);

            var monthlyWorkingDays = CalculateMonthlyWorkingDays(firstNewMonthsRevenueRecords);

            // gather average for baseActiveClients, baseSessionPrice, baseSessionsPerMonth, sessionMonthlyGrowth
            var trainerStatistics = GenerateTrainerRevenueStatistics(monthlyRecords, monthlyWorkingDays);

            var weeklyMultipliers = CalculateWeekdayMultiplier(allRevenueRecords);

            // if there is at least 3 month of data
            // - calculate churn rate & acqusition rate - rather than looking at totalActiveClients at the end of the month
            // else use historical standards


            // -48 in order to ensure exact 48 months output
            // null = MonthlyRevenuePatterns

            var revenueRecords = DummyDataGenerator.GenerateExtendedRevenueData(trainerStatistics, null, weeklyMultipliers, trainerId, 48 - monthlyRecords.Count);

            return firstRevenueRecord!;

        }

        private TrainerStatistics GenerateTrainerRevenueStatistics(List<TrainerDailyRevenue> revenueRecords ,int workingDays)
        {
            var activeClients = Math.Round(revenueRecords.Average(r => r.ActiveClients), 0);

            var sessionPricing = Math.Round(revenueRecords.Average(r => r.AverageSessionPrice), 0);

            var monthlySessions = Math.Round(revenueRecords.Average(r => r.TotalSessionsThisMonth), 0);

            var monthlyGrowth = CalculateSessionMonthlyGrowth(revenueRecords.Select(r => r.TotalSessionsThisMonth).ToList());

            var monthlyWorkingDays = workingDays;

            var statistics = new TrainerStatistics
            {
                BaseActiveClients = (int)activeClients,
                BaseSessionsPrice = sessionPricing,
                BaseSessionsPerMonth = (int)monthlySessions,
                SessionMonthlyGrowth = monthlyGrowth,
                MonthlyWorkingDays = monthlyWorkingDays
            };
            return statistics;
        }

        private double CalculateSessionMonthlyGrowth(List<int> monthlySessions)
        {
            var totalPercentageChanges = 0;

            for(int i = 0; i < monthlySessions.Count - 1; i++)
            {
                totalPercentageChanges += (monthlySessions[i + 1] - monthlySessions[i]) / monthlySessions[i] * 100;
            }
            // if no variance is present, add a slight increase
            if (totalPercentageChanges == 0) return 0.05;

            return totalPercentageChanges / (monthlySessions.Count - 1);
        }

        private int CalculateMonthlyWorkingDays(List<TrainerDailyRevenue> fullMonthRecords)
        {
            int workingDays = 0;

            foreach(var record in fullMonthRecords)
            {
                if(record.RevenueToday > 0)
                {
                    workingDays++;
                }
            }
            return workingDays;
        }

        private Dictionary<DayOfWeek, double> CalculateWeekdayMultiplier(List<TrainerDailyRevenue> allrevenueRecords)
        {
            double averageSessions = CalculateAverageDailySessions(allrevenueRecords);

            decimal averageSessionPrice = allrevenueRecords.FirstOrDefault()!.AverageSessionPrice;


            // gather all sessions for each specific weekday / by the number of that weekdays occurances for an average
            var weekdayAverages = allrevenueRecords
                .GroupBy(r => r.AsOfDate.DayOfWeek)
                .ToDictionary(
                g => g.Key,
                g => g.Average(r => (double)(r.RevenueToday / averageSessionPrice))
                );

            // use a formula to get a weekday multiplier of sorts e.g.  weeklyMultiplier = (weekdayAvg / overallAvg) 
            var multipliers = new Dictionary<DayOfWeek, double>();
            
            foreach(var day in weekdayAverages)
            {
                multipliers[day.Key] = day.Value / averageSessions;
            }

            return multipliers;
        }

        private double CalculateAverageDailySessions(List<TrainerDailyRevenue> revenueRecords)
        {
            var allSessions = revenueRecords.Select(r => r.RevenueToday).Sum() / revenueRecords.FirstOrDefault()!.Trainer.AverageSessionPrice;
            if (allSessions is null) return 0;

            return (double)allSessions / revenueRecords.Count;
        }

    }
}
