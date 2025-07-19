namespace ClientDashboard_API.Interfaces
{
    public interface ISessionSyncService
    {
        Task<bool> SyncDailySessions();
    }
}
