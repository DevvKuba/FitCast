using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API.Data
{
    public class UnitOfWork(DbContext context, IClientDataRepository clientDataRepository) : IUnitOfWork
    {
        public IClientDataRepository ClientDataRepository => clientDataRepository;

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
