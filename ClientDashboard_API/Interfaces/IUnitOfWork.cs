namespace ClientDashboard_API.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository UserRepository { get; }
        IClientRepository ClientRepository { get; }

        IWorkoutRepository WorkoutRepository { get; }

        ITrainerRepository TrainerRepository { get; }

        INotificationRepository NotificationRepository { get; }

        IPaymentRepository PaymentRepository { get; }

        IClientDailyFeatureRepository ClientDailyFeatureRepository { get; }

        ITrainerDailyRevenueRepository TrainerDailyRevenueRepository { get; }

        Task<bool> Complete();

        bool HasChanges();
    }
}
