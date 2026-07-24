namespace ClientDashboard_API.Interfaces
{
    public interface IVerifyEmail
    {
        Task<bool> Handle(int tokenId);
    }
}
