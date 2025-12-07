using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Data
{
    public class TrainerDailyRevenueRepository(DataContext context) : ITrainerDailyRevenueRepository
    {
        public async Task AddTrainerDailyRevenueRecordAsync(TrainerDailyDataAddDto trainerInfo)
        {
            var trainerRevenueRecord = new TrainerDailyRevenue
            {
                RevenueToday = trainerInfo.RevenueToday,
                MonthlyRevenueThusFar = trainerInfo.MonthlyRevenueThusFar,
                TotalSessionsThisMonth = trainerInfo.TotalSessionsThisMonth,
                NewClientsThisMonth = trainerInfo.NewClientsThisMonth,
                ActiveClients = trainerInfo.ActiveClients,
                AverageSessionPrice = trainerInfo.AverageSessionPrice,
                AsOfDate = trainerInfo.AsOfDate,
            };
            await context.TrainerDailyRevenue.AddAsync(trainerRevenueRecord);
        }
    }
}
