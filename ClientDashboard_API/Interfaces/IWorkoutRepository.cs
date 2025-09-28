using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IWorkoutRepository
    {
        Task<List<Workout>> GetWorkoutsAsync();
        Task<List<Workout>> GetClientWorkoutsAtDateAsync(DateOnly workoutDate);

        Task<Workout> GetClientWorkoutAtDateAsync(string clientName, DateOnly workoutDate);

        Task<List<Workout>> GetClientWorkoutsFromDateAsync(DateOnly workoutDate);

        Task<Workout> GetLatestClientWorkoutAsync(string clientName);

        Task AddWorkoutAsync(Client client, string workoutTitle, DateOnly workoutDate, int exerciseCount);

        // to be changed possibly
        void RemoveWorkout(Workout workout);

        // expand in future - more fields in Workout will allow for 
        // further data retrieval 

    }
}
