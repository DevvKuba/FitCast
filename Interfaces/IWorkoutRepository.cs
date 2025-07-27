using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IWorkoutRepository
    {
        Task<List<Workout>> GetClientWorkoutsByDateAsync(DateOnly date);

        Task<Workout> GetLatestClientWorkout(string clientName);

    }
}
