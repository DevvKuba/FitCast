using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Services
{
    public class TrainerDailyRevenueService(IUnitOfWork unitOfWork) : ITrainerDailyRevenueService
    {

        // revenue today would just be getting all confirmed trainer payments form todays Date
        // retrieving their amount

        // MonthlyRevenueThusFar - need to identify the month, check from the first - Current Date
        // all the confirmed payments

        // TotalSessionsThisMonth - same thing identify the 1t of current month
        // check for all clients under trainer, how many logged sessions are in between 1st - Current Date

        // NewClientsThisMonth - get access to the past month - client count and get get current client count
        // current - past = res

        // active clients , all clients with active status under trainer

        // average session price just takes the trainers set price - no need for excessive calculations

        public async Task ExecuteTrainerDailyRevenueGatheringAsync(Trainer trainer)
        {
            var todaysDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var firstDayOfTodaysMonth = GatherFirstDayOfCurrentMonth(todaysDate);


            var totalRevenueToday = CalculateTotalClientGeneratedRevenueAtDate(trainer, todaysDate);

            var monthlyRevenueThusFar = CalculateTotalClientGeneratedRevenueBetweenDates(trainer, firstDayOfTodaysMonth, todaysDate);
            var totalSessionThisMonth = ReturnMonthlyClientSessionsThusFar(trainer, firstDayOfTodaysMonth, todaysDate);

            var newClientsThisMonth = CalculateClientMonthlyDifference(trainer, todaysDate);

            var currentActiveClientsList = await unitOfWork.TrainerRepository.GetTrainerActiveClientsAsync(trainer);

            var trainerInfo = new TrainerDailyDataAddDto
            {
                TrainerId = trainer.Id,
                RevenueToday = totalRevenueToday,
                MonthlyRevenueThusFar = monthlyRevenueThusFar,
                TotalSessionsThisMonth = totalSessionThisMonth,
                NewClientsThisMonth = newClientsThisMonth,
                ActiveClients = currentActiveClientsList.Count,
                AverageSessionPrice = trainer.AverageSessionPrice ?? 0m,
                AsOfDate = todaysDate
            };

            await unitOfWork.TrainerDailyRevenueRepository.AddTrainerDailyRevenueRecordAsync(trainerInfo);
            await unitOfWork.Complete();

        }

        public decimal CalculateTotalClientGeneratedRevenueAtDate(Trainer trainer, DateOnly dateForSessions)
        {
            var clientsWorkouts = trainer.Clients.Select(c => c.Workouts).ToList();

            var workoutsToday = clientsWorkouts.SelectMany(w => w.Where(w => w.SessionDate == dateForSessions)).ToList();

            return workoutsToday.Count * trainer.AverageSessionPrice ?? 0m;
        }

        public decimal CalculateTotalClientGeneratedRevenueBetweenDates(Trainer trainer, DateOnly startDate, DateOnly endDate)
        {
            var clientsWorkouts = trainer.Clients.Select(c => c.Workouts).ToList();

            var workoutsToday = clientsWorkouts.SelectMany(w => w.Where(w => w.SessionDate >= startDate && w.SessionDate <= endDate)).ToList();

            return workoutsToday.Count * trainer.AverageSessionPrice ?? 0m;
        }

        public int CalculateClientMonthlyDifference(Trainer trainer, DateOnly currentDate)
        {
            var lastDayOfPreviousMonth = new DateOnly(currentDate.Year, currentDate.Month - 1, -1);
            var clientsLastMonth = trainer.Clients.Where(c => DateOnly.FromDateTime(c.CreatedAt) <= lastDayOfPreviousMonth).Count();

            var clientsThisMonth = trainer.Clients.Count;

            return clientsThisMonth - clientsLastMonth;

    
        }

        public int ReturnMonthlyClientSessionsThusFar(Trainer trainer, DateOnly startDate, DateOnly endDate)
        {
            var clientsWorkouts = trainer.Clients.Select(c => c.Workouts).ToList();

            var workoutsToday = clientsWorkouts.SelectMany(w => w.Where(w => w.SessionDate >= startDate && w.SessionDate <= endDate)).ToList();

            return workoutsToday.Count;
        }

        public DateOnly GatherFirstDayOfCurrentMonth(DateOnly currentDate)
        {
            var firstDayOfGivenMonth = new DateOnly(currentDate.Year, currentDate.Month, 1);
            return firstDayOfGivenMonth;
        }

    }
}
