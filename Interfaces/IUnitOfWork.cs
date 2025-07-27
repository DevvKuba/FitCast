namespace ClientDashboard_API.Interfaces
{
    public interface IUnitOfWork
    {
        IClientRepository ClientRepository { get; }

        IWorkoutRepository WorkoutRepository { get; }

        Task<bool> Complete();

        bool HasChanges();
    }
}
