using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ISessionSyncService
    {
        Task<bool> SyncDailyPipelineSessionsAsync();

        Task<bool> SyncSessionsAsync(Trainer trainer);
    }
}
