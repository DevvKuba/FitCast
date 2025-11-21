using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Data
{
    public class UnitOfWork(DataContext context, IClientRepository clientRepository, IWorkoutRepository workoutRepository,
        ITrainerRepository trainerRepository, INotificationRepository notificationRepository, IPaymentRepository paymentRepository) : IUnitOfWork
    {
        public IClientRepository ClientRepository => clientRepository;

        public IWorkoutRepository WorkoutRepository => workoutRepository;

        public ITrainerRepository TrainerRepository => trainerRepository;

        public INotificationRepository NotificationRepository => notificationRepository;

        public IPaymentRepository paymentRepository => paymentRepository;

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
