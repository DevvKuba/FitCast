using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Data
{
    public class UnitOfWork(DataContext context, IUserRepository userRepository, IClientRepository clientRepository, IWorkoutRepository workoutRepository,
        ITrainerRepository trainerRepository, INotificationRepository notificationRepository,
        IPaymentRepository paymentRepository , IEmailVerificationTokenRepository emailVerificationTokenRepository, 
        IClientDailyFeatureRepository clientDailyFeatureRepository, ITrainerDailyRevenueRepository trainerDailyRevenueRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository  
        ) : IUnitOfWork
    {
        public IUserRepository UserRepository => userRepository;
        public IClientRepository ClientRepository => clientRepository;

        public IWorkoutRepository WorkoutRepository => workoutRepository;

        public ITrainerRepository TrainerRepository => trainerRepository;

        public INotificationRepository NotificationRepository => notificationRepository;

        public IPaymentRepository PaymentRepository => paymentRepository;

        public IEmailVerificationTokenRepository EmailVerificationTokenRepository => emailVerificationTokenRepository;

        public IPasswordResetTokenRepository PasswordResetTokenRepository => passwordResetTokenRepository;

        public IClientDailyFeatureRepository ClientDailyFeatureRepository => clientDailyFeatureRepository;

        public ITrainerDailyRevenueRepository TrainerDailyRevenueRepository => trainerDailyRevenueRepository;

        public async Task<bool> Complete()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public bool HasChanges()
        {
            // returns true if there are any tracker entities in the context that have been modified
            return context.ChangeTracker.HasChanges();
        }
    }
}
