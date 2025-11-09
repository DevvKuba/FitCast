namespace ClientDashboard_API.Interfaces
{
    public interface ISessionSyncService
    {
        Task<bool> SyncDailyPipelineSessionsAsync();

        Task<bool> SyncSessionsAsync(int trainerId);
    }
}
