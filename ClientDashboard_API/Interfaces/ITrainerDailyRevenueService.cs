using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerDailyRevenueService
    {
        Task ExecuteTrainerDailyRevenueGatheringAsync(Trainer trainer);

        DateOnly GatherFirstDayOfCurrentMonth(DateOnly currentDate);
    }
}
