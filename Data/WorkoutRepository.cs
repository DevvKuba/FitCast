using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class WorkoutRepository(DataContext context, IClientRepository clientRepository) : IWorkoutRepository
    {
        public async Task<Workout> GetSpecificClientWorkoutAsync(DateOnly workoutDate, string clientName)
        {
            Workout? clientWorkout = await context.Workouts.Where(x => x.ClientName == clientName && x.SessionDate == workoutDate).FirstOrDefaultAsync();
            return clientWorkout;
        }
        public async Task<List<Workout>> GetClientWorkoutsByDateAsync(DateOnly workoutDate)
        {
            List<Workout?> clientData = await context.Workouts.Where(x => x.SessionDate == workoutDate).ToListAsync();
            return clientData;
        }

        public async Task<Workout> GetLatestClientWorkoutAsync(string clientName)
        {
            Workout? clientWorkout = await context.Workouts.Where(x => x.ClientName == clientName.ToLower()).FirstOrDefaultAsync();
            return clientWorkout;
        }

        public async Task AddWorkoutAsync(Workout workout)
        {
            var clientData = await clientRepository.GetClientByNameAsync(workout.ClientName);
            clientData.Workouts.Add(workout);
        }

        public async Task RemoveWorkoutAsync(Workout workout)
        {
            var clientData = await clientRepository.GetClientByNameAsync(workout.ClientName);
            clientData.Workouts.Remove(workout);
        }
    }
}
