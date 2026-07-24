using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Helpers
{
    public interface IPasswordResetLinkFactory
    {
        string Create(string rawToken);
    }
}
