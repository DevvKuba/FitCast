using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IEmailVerificationLinkFactory
    {
        string Create(EmailVerificationToken emailVerificationToken);
    }
}
