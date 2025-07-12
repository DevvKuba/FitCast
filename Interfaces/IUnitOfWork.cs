namespace ClientDashboard_API.Interfaces
{
    public interface IUnitOfWork
    {
        IClientDataRepository ClientDataRepository { get; }

        Task<bool> Complete();

        bool HasChanges();
    }
}
