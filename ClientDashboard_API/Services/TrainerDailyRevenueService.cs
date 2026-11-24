using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Eventing.Reader;

namespace ClientDashboard_API.Services
{
    public class TrainerDailyRevenueService(IUnitOfWork unitOfWork) : ITrainerDailyRevenueService
    {
        public async Task ExecuteTrainerDailyRevenueGatheringAsync(Trainer trainer)
        {
            // runs straight after the session sync, so the trainer's yesterday is complete
            var snapshotDate = TrainerLocalDateHelper.GetYesterday(trainer.TimeZoneId, DateTime.UtcNow);
            var firstDayOfSnapshotMonth = GatherFirstDayOfCurrentMonth(snapshotDate);

            var totalRevenueToday = await CalculateTotalClientGeneratedRevenueAtDateAsync(trainer, snapshotDate);

            var monthlyRevenueThusFar = await CalculateTotalClientGeneratedRevenueBetweenDatesAsync(trainer, firstDayOfSnapshotMonth, snapshotDate);

            var sessionsToday = await unitOfWork.WorkoutRepository.GetClientWorkoutCountForTrainerAtDateAsync(trainer, snapshotDate);

            var totalSessionsThisMonth = await ReturnMonthlyClientSessionsThusFarAsync(trainer, firstDayOfSnapshotMonth, snapshotDate);
            
            var newClientsThisMonth = CalculateClientMonthlyDifference(trainer, snapshotDate);

            var totalSessionDuration = await unitOfWork.WorkoutRepository.GetTotalClientSessionDurationAtDateAsync(trainer, snapshotDate);

            var currentActiveClientsList = await unitOfWork.TrainerRepository.GetTrainerActiveClientsAsync(trainer);

            var trainerInfo = new TrainerDailyRevenueDto
            {
                TrainerId = trainer.Id,
                RevenueToday = totalRevenueToday,
                MonthlyRevenueThusFar = monthlyRevenueThusFar,
                SessionsToday = sessionsToday,
                TotalSessionsThisMonth = totalSessionsThisMonth,
                NewClientsThisMonth = newClientsThisMonth,
                TotalSessionDuration = totalSessionDuration,
                ActiveClients = currentActiveClientsList.Count,
                AverageSessionPrice = trainer.AverageSessionPrice ?? 0m,
                AsOfDate = snapshotDate
            };

            if (await unitOfWork.TrainerDailyRevenueRepository.DoesTrainerDailyRevenueRecordExistForDateAsync(trainerInfo.TrainerId, trainerInfo.AsOfDate)) 
            {
                await unitOfWork.TrainerDailyRevenueRepository.UpdateTrainerRevenueRecordAtDateAsync(trainerInfo);
            }
            else
            {
                try
                {
                    await unitOfWork.TrainerDailyRevenueRepository.AddTrainerDailyRevenueDtoRecordAsync(trainerInfo);
                    await unitOfWork.Complete();
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique key") ?? false)
                {
                    unitOfWork.Clear();
                    await unitOfWork.TrainerDailyRevenueRepository.UpdateTrainerRevenueRecordAtDateAsync(trainerInfo);
                }
            }
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
