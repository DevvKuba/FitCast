using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Helpers
{
    public interface ITokenProvider
    {
        string Create(UserBase user);
    }
}
