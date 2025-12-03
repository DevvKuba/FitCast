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
            var clientData = await context.Workouts.Where(x => x.SessionDate == workoutDate).ToListAsync();
            return clientData;
        }

        public async Task<Workout?> GetClientWorkoutAtDateByNameAsync(string clientName, DateOnly workoutDate)
        {
            Workout? clientData = await context.Workouts.Where(x => x.SessionDate == workoutDate && x.ClientName == clientName.ToLower()).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<Workout?> GetClientWorkoutAtDateByIdAsync(int clientId, DateOnly workoutDate)
        {
            Workout? clientData = await context.Workouts.Where(x => x.SessionDate == workoutDate && x.ClientId == clientId).FirstOrDefaultAsync();
            return clientData;
        }

        public async Task<List<Workout>> GetClientWorkoutsFromDateAsync(DateOnly workoutDate)
        {
            List<Workout> clientData = await context.Workouts.Where(x => x.SessionDate >= workoutDate).ToListAsync();
            return clientData;
        }

        public async Task<Workout?> GetLatestClientWorkoutAsync(string clientName)
        {
            Workout? clientWorkout = await context.Workouts.Where(x => x.ClientName == clientName.ToLower()).OrderByDescending(x => x.SessionDate).FirstOrDefaultAsync();
            return clientWorkout;
        }

        public async Task<int> GetSessionCountAsync(Client client, DateOnly fromDate, DateOnly untilDate)
        {
            var workoutsBetweenDates = await context.Workouts.Where(w => w.SessionDate >= fromDate && w.SessionDate <= untilDate && w.ClientId == client.Id).ToListAsync();
            return workoutsBetweenDates.Count;
        }

        public async Task<int> GetSessionCountLast7DaysAsync(Client client, DateOnly untilDate)
        {
            var dateFrom7DaysAgo = untilDate.AddDays(-7);

            var workoutsTillDate = await context.Workouts.Where(w => w.SessionDate >= dateFrom7DaysAgo && w.SessionDate <= untilDate && w.ClientId == client.Id).ToListAsync();
            return workoutsTillDate.Count;
        }

        public async Task<int> GetSessionCountLast28DaysAsync(Client client, DateOnly untilDate)
        {
            var dateFrom7DaysAgo = untilDate.AddDays(-28);

            var workoutsTillDate = await context.Workouts.Where(w => w.SessionDate >= dateFrom7DaysAgo && w.SessionDate <= untilDate && w.ClientId == client.Id).ToListAsync();
            return workoutsTillDate.Count;
        }

        public async Task<int?> GetDaysFromLastSessionAsync(Client client, DateOnly untilDate)
        {
            var lastSession = await context.Workouts
                .Where(w => w.ClientId == client.Id)
                .OrderByDescending(w => w.SessionDate)
                .FirstOrDefaultAsync();

            int? daysFromLastSession = lastSession == null ? null : untilDate.DayNumber - lastSession.SessionDate.DayNumber;
            return daysFromLastSession;

        }

        public void UpdateWorkout(Workout existingWorkout, string workoutTitle, DateOnly sessionDate, int exerciseCount, int duration)
        {
            existingWorkout.WorkoutTitle = workoutTitle;
            existingWorkout.SessionDate = sessionDate;
            existingWorkout.ExerciseCount = exerciseCount;
            existingWorkout.Duration = duration;

        }

        public async Task<int> CalculateClientMeanWorkoutDurationAsync(Client client, DateOnly tillDate)
        {
           var workoutsTillDate = await context.Workouts.Where(w => w.SessionDate <= tillDate && w.ClientId == client.Id).ToListAsync();

            int meanDuration = workoutsTillDate.Count == 0 ? 0 : (int)Math.Round(workoutsTillDate.Select(w => w.Duration).Average());
            return meanDuration;
        }


        public async Task AddWorkoutAsync(Client client, string workoutTitle, DateOnly workoutDate, int exerciseCount, int duration)
        {
            await context.Workouts.AddAsync(new Workout
            {
                ClientId = client.Id,
                ClientName = client.FirstName,
                WorkoutTitle = workoutTitle,
                SessionDate = workoutDate,
                CurrentBlockSession = client.CurrentBlockSession,
                TotalBlockSessions = client.TotalBlockSessions,
                ExerciseCount = exerciseCount,
                Duration = duration,
                Client = client
            });
        }

        public void RemoveWorkout(Workout workout)
        {
            context.Workouts.Remove(workout);
        }
    }
}
