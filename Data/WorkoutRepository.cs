using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class WorkoutRepository(DataContext context) : IWorkoutRepository
    {
        public async Task<List<Workout>> GetClientWorkoutsByDateAsync(DateOnly date)
        {
            var clientData = await context.Workouts.Where(x => x.SessionDate == date).ToListAsync();
            return clientData;
        }

        public async Task<Workout> GetLatestClientWorkout(string clientName)
        {
            var clientWorkout = await context.Workouts.Where(x => x.ClientName == clientName.ToLower()).FirstOrDefaultAsync();
            return clientWorkout;
        }
    }
}
