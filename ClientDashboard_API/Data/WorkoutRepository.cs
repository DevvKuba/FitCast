using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class WorkoutRepository(DataContext context) : IWorkoutRepository
    {
        public List<Workout> GetSpecificClientWorkoutsAsync(List<Client> clientList)
        {
            // is there a better way to do this
            List<Workout> workouts = [];
            foreach (Client client in clientList)
            {
                foreach (Workout workout in client.Workouts)
                {
                    workouts.Add(workout);
                }
            }
            // workouts that were provided with an inclusion to the client, that inclusion
            // should be cut off to remove the circular reference between entites

            workouts = workouts
                .OrderByDescending(x => x.SessionDate).ToList();

            return workouts;
        }
        public async Task<List<Workout>> GetClientWorkoutsAtDateAsync(DateOnly workoutDate)
        {
            List<Workout?> clientData = await context.Workouts.Where(x => x.SessionDate == workoutDate).ToListAsync();
            return clientData;
        }

        public async Task<Workout> GetClientWorkoutAtDateAsync(string clientName, DateOnly workoutDate)
        {
            Workout? clientData = await context.Workouts.Where(x => x.SessionDate == workoutDate && x.ClientName == clientName.ToLower()).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<List<Workout>> GetClientWorkoutsFromDateAsync(DateOnly workoutDate)
        {
            List<Workout> clientData = await context.Workouts.Where(x => x.SessionDate >= workoutDate).ToListAsync();
            return clientData;
        }

        public async Task<Workout> GetLatestClientWorkoutAsync(string clientName)
        {
            Workout? clientWorkout = await context.Workouts.Where(x => x.ClientName == clientName.ToLower()).OrderByDescending(x => x.SessionDate).FirstOrDefaultAsync();
            return clientWorkout;
        }


        public async Task AddWorkoutAsync(Client client, string workoutTitle, DateOnly workoutDate, int exerciseCount)
        {
            await context.Workouts.AddAsync(new Workout
            {
                ClientId = client.Id,
                ClientName = client.Name,
                WorkoutTitle = workoutTitle,
                SessionDate = workoutDate,
                CurrentBlockSession = client.CurrentBlockSession,
                TotalBlockSessions = client.TotalBlockSessions,
                ExerciseCount = exerciseCount,
                Client = client
            });
        }

        public void RemoveWorkout(Workout workout)
        {
            context.Workouts.Remove(workout);
        }

    }
}
