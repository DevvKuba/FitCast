using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ISessionSyncService
    {
        Task<int> SyncSessionsAsync(Trainer trainer);
    }
}
