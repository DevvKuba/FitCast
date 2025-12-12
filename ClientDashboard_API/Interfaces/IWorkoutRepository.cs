using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IWorkoutRepository
    {
        List<Workout> GetSpecificClientsWorkoutsAsync(List<Client> clientList);
        Task<List<Workout>> GetClientWorkoutsAtDateAsync(DateOnly workoutDate);

        Task<Workout?> GetWorkoutByIdAsync(int id);

        Task<Workout?> GetClientWorkoutAtDateByNameAsync(string clientName, DateOnly workoutDate);

        Task<Workout?> GetClientWorkoutAtDateByIdAsync(int id, DateOnly workoutDate);

        Task<List<Workout>> GetClientWorkoutsFromDateAsync(DateOnly workoutDate);

        Task<Workout?> GetLatestClientWorkoutAsync(string clientName);

        Task<int> GetSessionCountAsync(Client client, DateOnly fromDate, DateOnly untilDate);

        Task<int> GetSessionCountLast7DaysAsync(Client client, DateOnly untilDate);

        Task<int> GetSessionCountLast28DaysAsync(Client client, DateOnly untilDate);

        Task<int?> GetDaysFromLastSessionAsync(Client client, DateOnly untilDate);

        Task<int> CalculateClientMeanWorkoutDurationAsync(Client client, DateOnly tillDate);

        void UpdateWorkout(Workout existingWorkout, string workoutTitle, DateOnly sessionDate, int exerciseCount, int duration);

        Task AddWorkoutAsync(Client client, string workoutTitle, DateOnly workoutDate, int exerciseCount, int duration);

        void RemoveWorkout(Workout workout);

        // expand in future - more fields in Workout will allow for 
        // further data retrieval 

    }
}
