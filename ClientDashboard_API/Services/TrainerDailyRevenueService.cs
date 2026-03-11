using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class TrainerDailyRevenueService(IUnitOfWork unitOfWork) : ITrainerDailyRevenueService
    {

        public async Task ExecuteTrainerDailyRevenueGatheringAsync(Trainer trainer)
        {
            var todaysDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var firstDayOfTodaysMonth = GatherFirstDayOfCurrentMonth(todaysDate);

            var totalRevenueToday = await CalculateTotalClientGeneratedRevenueAtDateAsync(trainer, todaysDate);

            var monthlyRevenueThusFar = await CalculateTotalClientGeneratedRevenueBetweenDatesAsync(trainer, firstDayOfTodaysMonth, todaysDate);

            var totalSessionsThisMonth = await ReturnMonthlyClientSessionsThusFarAsync(trainer, firstDayOfTodaysMonth, todaysDate);

            var newClientsThisMonth = CalculateClientMonthlyDifference(trainer, todaysDate);

            var currentActiveClientsList = await unitOfWork.TrainerRepository.GetTrainerActiveClientsAsync(trainer);

            var trainerInfo = new TrainerDailyDataAddDto
            {
                TrainerId = trainer.Id,
                RevenueToday = totalRevenueToday,
                MonthlyRevenueThusFar = monthlyRevenueThusFar,
                TotalSessionsThisMonth = totalSessionsThisMonth,
                NewClientsThisMonth = newClientsThisMonth,
                ActiveClients = currentActiveClientsList.Count,
                AverageSessionPrice = trainer.AverageSessionPrice ?? 0m,
                AsOfDate = todaysDate
            };

            await unitOfWork.TrainerDailyRevenueRepository.AddTrainerDailyRevenueRecordAsync(trainerInfo);
            await unitOfWork.Complete();

        }

        public async Task<decimal> CalculateTotalClientGeneratedRevenueAtDateAsync(Trainer trainer, DateOnly dateForSessions)
        {
            var relatedWorkouts = await unitOfWork.WorkoutRepository.GetAllWorkoutsAssociatedWithTrainerIgnoringQueryFiltersAsync(trainer);

            var workoutsToday = relatedWorkouts.Where(w => w.SessionDate == dateForSessions).ToList();

            return workoutsToday.Count * (trainer.AverageSessionPrice ?? 0m);
        }

        public async Task<decimal> CalculateTotalClientGeneratedRevenueBetweenDatesAsync(Trainer trainer, DateOnly startDate, DateOnly endDate)
        {
            var relatedWorkouts = await unitOfWork.WorkoutRepository.GetAllWorkoutsAssociatedWithTrainerIgnoringQueryFiltersAsync(trainer);

            var workoutsThisMonth = relatedWorkouts.Where(w => w.SessionDate >= startDate && w.SessionDate <= endDate).ToList();

            return workoutsThisMonth.Count * (trainer.AverageSessionPrice ?? 0m);
        }

        public int CalculateClientMonthlyDifference(Trainer trainer, DateOnly currentDate)
        {
            var lastDayOfPreviousMonth = currentDate.AddDays(-currentDate.Day);

            var clientsLastMonth = trainer.Clients.Where(c => DateOnly.FromDateTime(c.CreatedAt) <= lastDayOfPreviousMonth).Count();

            var clientsThisMonth = trainer.Clients.Count;

            return clientsThisMonth - clientsLastMonth;

    
        }

        public async Task<int> ReturnMonthlyClientSessionsThusFarAsync(Trainer trainer, DateOnly startDate, DateOnly endDate)
        {

            var relatedWorkouts = await unitOfWork.WorkoutRepository.GetAllWorkoutsAssociatedWithTrainerIgnoringQueryFiltersAsync(trainer);

            var workoutsThisMonth = relatedWorkouts.Where(w => w.SessionDate >= startDate && w.SessionDate <= endDate).ToList();

            return workoutsThisMonth.Count;
        }

        public DateOnly GatherFirstDayOfCurrentMonth(DateOnly currentDate)
        {
            var firstDayOfGivenMonth = new DateOnly(currentDate.Year, currentDate.Month, 1);
            return firstDayOfGivenMonth;
        }

    }
}
