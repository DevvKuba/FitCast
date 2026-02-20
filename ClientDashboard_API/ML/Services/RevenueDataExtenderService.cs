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

            // a month from the first recorded trainer daily revenue record
            var monthlyRecords = await unitOfWork.TrainerDailyRevenueRepository.GetLastMonthsDayRecordsBasedOnFirstRecordAsync(firstRevenueRecord!);

            // gather average for baseActiveClients, baseSessionPrice, baseSessionsPerMonth, sessionMonthlyGrowth
            var trainerStatistics = GenerateTrainerRevenueStatistics(monthlyRecords);

            var revenueRecords = DummyDataGenerator.GenerateExtendedRevenueData(trainerStatistics, trainerId, 48 - monthlyRecords.Count);

            return firstRevenueRecord!;

        }

        private TrainerStatistics GenerateTrainerRevenueStatistics(List<TrainerDailyRevenue> revenueRecords)
        {
            var activeClients = Math.Round(revenueRecords.Average(r => r.ActiveClients), 0);

            var sessionPricing = Math.Round(revenueRecords.Average(r => r.AverageSessionPrice), 0);

            var monthlySessions = Math.Round(revenueRecords.Average(r => r.TotalSessionsThisMonth), 0);

            var monthlyGrowth = CalculateSessionMonthlyGrowth(revenueRecords.Select(r => r.TotalSessionsThisMonth).ToList());

            var statistics = new TrainerStatistics
            {
                BaseActiveClients = (int)activeClients,
                BaseSessionsPrice = sessionPricing,
                BaseSessionsPerMonth = (int)monthlySessions,
                SessionMonthlyGrowth = monthlyGrowth
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

    }
}
