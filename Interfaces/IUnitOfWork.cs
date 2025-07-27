namespace ClientDashboard_API.Interfaces
{
    public interface IUnitOfWork
    {
        IClientRepository ClientDataRepository { get; }

        IWorkoutRepository WorkoutRepository { get; }

        Task<bool> Complete();

        bool HasChanges();
    }
}
