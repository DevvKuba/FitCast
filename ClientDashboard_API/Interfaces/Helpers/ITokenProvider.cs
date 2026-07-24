using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface ITokenProvider
    {
        string Create(UserBase user);
    }
}
