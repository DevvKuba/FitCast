namespace ClientDashboard_API.Interfaces
{
    public interface IUnitOfWork
    {
        IClientRepository ClientRepository { get; }

        IWorkoutRepository WorkoutRepository { get; }

        ITrainerRepository TrainerRepository { get; }

        INotificationRepository NotificationRepository { get; }

        Task<bool> Complete();

        bool HasChanges();
    }
}
