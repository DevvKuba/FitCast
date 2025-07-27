namespace ClientDashboard_API.Interfaces
{
    public interface IUnitOfWork
    {
        IClientDataRepository ClientDataRepository { get; }

        IWorkoutRepository WorkoutRepository { get; }

        Task<bool> DbUpdateComplete();

        bool HasChanges();
    }
}
