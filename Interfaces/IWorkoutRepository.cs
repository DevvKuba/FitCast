using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IWorkoutRepository
    {
        Task<List<Workout>> GetClientWorkoutsAtDateAsync(DateOnly workoutDate);

        Task<Workout> GetClientWorkoutAtDateAsync(string clientName, DateOnly workoutDate);

        Task<List<Workout>> GetClientWorkoutsFromDateAsync(DateOnly workoutDate);

        Task<Workout> GetSpecificClientWorkoutAsync(DateOnly workoutDate, string clientName);

        Task<Workout> GetLatestClientWorkoutAsync(string clientName);

        Task AddWorkoutAsync(Workout workout);

        Task RemoveWorkoutAsync(Workout workout);

        // expand in future - more fields in Workout will allow for 
        // further data retrieval 

    }
}
