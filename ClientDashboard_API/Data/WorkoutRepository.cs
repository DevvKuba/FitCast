using AutoMapper;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class WorkoutRepository(DataContext context, IMapper mapper) : IWorkoutRepository
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
            workouts = workouts.OrderByDescending(x => x.SessionDate).ToList();

            // to remove the presence of a circular reference
            foreach (Workout workout in workouts)
            {
                workout.Client = null!;
            }

            return workouts;
        }

        public async Task<Workout?> GetWorkoutByIdAsync(int id)
        {
            Workout? workout = await context.Workouts.Where(x => x.Id == id).FirstOrDefaultAsync();
            return workout;
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

        public void UpdateWorkout(Workout existingWorkout, string workoutTitle, DateOnly sessionDate, int exerciseCount)
        {
            existingWorkout.WorkoutTitle = workoutTitle;
            existingWorkout.SessionDate = sessionDate;
            existingWorkout.ExerciseCount = exerciseCount;

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
