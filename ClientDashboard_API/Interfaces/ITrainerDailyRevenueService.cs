using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerDailyRevenueService
    {
        Task ExecuteTrainerDailyRevenueGatheringAsync(Trainer trainer);

        Task<decimal> CalculateTotalClientGeneratedRevenueAtDateAsync(Trainer trainer, DateOnly dateForSessions);

        Task<decimal> CalculateTotalClientGeneratedRevenueBetweenDatesAsync(Trainer trainer, DateOnly startDate, DateOnly endDate);

        int CalculateClientMonthlyDifference(Trainer trainer, DateOnly currentDate);

        Task<int> ReturnMonthlyClientSessionsThusFarAsync(Trainer trainer, DateOnly startDate, DateOnly endDate);

        DateOnly GatherFirstDayOfCurrentMonth(DateOnly currentDate);
    }
}
