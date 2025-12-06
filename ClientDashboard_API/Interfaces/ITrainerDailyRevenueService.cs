using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITrainerDailyRevenueService
    {
        Task ExecuteTrainerDailyRevenueGatheringAsync(Trainer trainer);

        decimal CalculateTotalClientGeneratedRevenueAtDate(Trainer trainer, DateOnly dateForSessions);

        decimal CalculateTotalClientGeneratedRevenueBetweenDates(Trainer trainer, DateOnly startDate, DateOnly endDate);

        int CalculateClientMonthlyDifference(Trainer trainer, DateOnly currentDate);

        int ReturnMonthlyClientSessionsThusFar(Trainer trainer, DateOnly startDate, DateOnly endDate);

        DateOnly GatherFirstDayOfCurrentMonth(DateOnly currentDate);
    }
}
