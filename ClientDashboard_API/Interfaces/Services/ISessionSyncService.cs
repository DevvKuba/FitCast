using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Services
{
    public interface ISessionSyncService
    {
        Task<int> SyncSessionsAsync(Trainer trainer);
    }
}
