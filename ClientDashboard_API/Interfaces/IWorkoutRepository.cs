using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IWorkoutRepository
    {
        List<Workout> GetSpecificClientWorkoutsAsync(List<Client> clientList);
        Task<List<Workout>> GetClientWorkoutsAtDateAsync(DateOnly workoutDate);

        Task<Workout?> GetWorkoutByIdAsync(int id);

        Task<Workout?> GetClientWorkoutAtDateByNameAsync(string clientName, DateOnly workoutDate);

        Task<Workout?> GetClientWorkoutAtDateByIdAsync(int id, DateOnly workoutDate);

        Task<List<Workout>> GetClientWorkoutsFromDateAsync(DateOnly workoutDate);

        Task<Workout?> GetLatestClientWorkoutAsync(string clientName);

        void UpdateWorkout(Workout existingWorkout, string workoutTitle, DateOnly sessionDate, int exerciseCount, int duration);

        Task AddWorkoutAsync(Client client, string workoutTitle, DateOnly workoutDate, int exerciseCount, int duration);

        void RemoveWorkout(Workout workout);

        // expand in future - more fields in Workout will allow for 
        // further data retrieval 

    }
}
