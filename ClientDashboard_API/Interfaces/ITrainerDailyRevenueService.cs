using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerDailyRevenueService
    {
        Task ExecuteTrainerDailyRevenueGatheringAsync(Trainer trainer);

        decimal CalculateTotalDailyClientGeneratedRevenue(Trainer trainer, List<Client> clients, DateOnly dateForSessions);

        DateOnly GatherFirstDayOfCurrentMonth(DateOnly currentDate);
    }
}
