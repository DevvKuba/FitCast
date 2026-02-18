using ClientDashboard_API.ML.Interfaces;
using ClientDashboard_API.ML.Models;

namespace ClientDashboard_API.ML.Services
{
    public class RevenueDataExtenderService : IRevenueDataExtenderService
    {
        public Task<List<TrainerRevenueData>> ProvideExtensionRecordsForRevenueDataAsync(int trainerId)
        {
            // 1
            // gather the last day of each months TrainerDailyRevenueRecords 

            // gather average for baseActiveClients, baseSessionPrice, baseSessionsPerMonth, sessionMonthlyGrowth

            // pass a dto with those properties into the newly declared dummyExtension method that extends more records based on real, current patterns
        }

        public Task FilterExtensionRevenueRecordsAsync(int trainerId)
        {
            // 2
            // delete all dummy extended data - leaving only the original records
        }

    }
}
