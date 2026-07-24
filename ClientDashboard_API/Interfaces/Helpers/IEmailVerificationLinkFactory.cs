using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces.Helpers
{
    public interface IEmailVerificationLinkFactory
    {
        string Create(string rawToken);
    }
}
