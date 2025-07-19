namespace ClientDashboard_API.Interfaces
{
    public interface IUnitOfWork
    {
        IClientDataRepository ClientDataRepository { get; }

        Task<bool> DbUpdateComplete();

        bool HasChanges();
    }
}
