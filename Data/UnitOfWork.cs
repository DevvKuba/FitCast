using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Data
{
    public class UnitOfWork(DataContext context, IClientRepository clientDataRepository, IWorkoutRepository workoutRepository) : IUnitOfWork
    {
        public IClientRepository ClientDataRepository => clientDataRepository;

        public IWorkoutRepository WorkoutRepository => workoutRepository;

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
