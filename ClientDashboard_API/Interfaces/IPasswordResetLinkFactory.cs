using ClientDashboard_API.Entities;

namespace ClientDashboard_API.Interfaces
{
    public interface IPasswordResetLinkFactory
    {
        string Create(PasswordResetToken passwordResetToken);
    }
}
