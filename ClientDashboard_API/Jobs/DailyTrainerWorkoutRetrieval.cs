using ClientDashboard_API.Data;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Quartz;

namespace ClientDashboard_API.Jobs
{
    public class DailyTrainerWorkoutRetrieval(DataContext dataContext, ISessionSyncService syncService) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            foreach(Trainer trainer in dataContext.Trainer)
            {
                if (trainer.AutoRetrieval)
                {
                    var workoutCount = await syncService.SyncSessionsAsync(trainer);
                    Console.WriteLine($"Retrieved {workoutCount} client workouts for trainer: {trainer.FirstName} at {DateTime.UtcNow}");
                }
            }
        }
    }
}
