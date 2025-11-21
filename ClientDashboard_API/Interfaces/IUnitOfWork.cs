namespace ClientDashboard_API.Interfaces
{
    public interface IUnitOfWork
    {
        IClientRepository ClientRepository { get; }

        IWorkoutRepository WorkoutRepository { get; }

        ITrainerRepository TrainerRepository { get; }

        INotificationRepository NotificationRepository { get; }

        IPaymentRepository PaymentRepository { get; }

        Task<bool> Complete();

        bool HasChanges();
    }
}
