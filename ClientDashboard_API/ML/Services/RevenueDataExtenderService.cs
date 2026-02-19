using ClientDashboard_API.Interfaces;
using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.ML.Services
{
    public class RevenueDataExtenderService(IUnitOfWork unitOfWork) : IRevenueDataExtenderService
    {
        public async Task ProvideExtensionRecordsForRevenueDataAsync(int trainerId)
        {
            // 1
            // gather the last day of each months TrainerDailyRevenueRecords 
            var trainerDailyRevenueRecords = await unitOfWork.TrainerDailyRevenueRepository.GetAllRevenueRecordsForTrainerAsync(trainerId);

            var firstRevenueRecord = await unitOfWork.TrainerDailyRevenueRepository.GetFirstRevenueRecordForTrainerAsync(trainerId);

            // a month from the first recorded trainer daily revenue record
            var monthlyRecords = await unitOfWork.TrainerDailyRevenueRepository.GetLastMonthsDayRecordsBasedOnFirstRecordAsync(firstRevenueRecord!);

            // gather average for baseActiveClients, baseSessionPrice, baseSessionsPerMonth, sessionMonthlyGrowth
            // if 1 just set if more than that need to calculate
            

            // pass the model with those properties into the newly declared dummyExtension method that extends more records based on real, current patterns
        }

        public Task FilterExtensionRevenueRecordsAsync(int trainerId)
        {
            // 2
            // delete all dummy extended data - leaving only the original records
        }

    }
}
