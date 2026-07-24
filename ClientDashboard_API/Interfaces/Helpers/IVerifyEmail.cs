namespace ClientDashboard_API.Interfaces.Helpers
{
    public interface IVerifyEmail
    {
        Task<bool> Handle(int tokenId);
    }
}
