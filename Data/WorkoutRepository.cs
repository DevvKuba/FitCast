using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class WorkoutRepository(DataContext context) : IWorkoutRepository
    {
        public async Task<Workout> GetSpecificClientWorkoutAsync(DateOnly workoutDate, string clientName)
        {
            var clientWorkout = await context.Workouts.Where(x => x.ClientName == clientName && x.SessionDate == workoutDate).FirstOrDefaultAsync();
            return clientWorkout;
        }
        public async Task<List<Workout>> GetClientWorkoutsByDateAsync(DateOnly workoutDate)
        {
            var clientData = await context.Workouts.Where(x => x.SessionDate == workoutDate).ToListAsync();
            return clientData;
        }

        public async Task<Workout> GetLatestClientWorkoutAsync(string clientName)
        {
            var clientWorkout = await context.Workouts.Where(x => x.ClientName == clientName.ToLower()).FirstOrDefaultAsync();
            return clientWorkout;
        }

        // call within the controller , search for given client then add the workout to their list of workouts
        //public async Task Add(Workout workout, string clientName)
        //{
        //    context.Workouts.Add(workout);
        //}

        //public async Task Remove(Workout workout)
        //{
        //    context.Workouts.Remove(workout);
        //}
    }
}
