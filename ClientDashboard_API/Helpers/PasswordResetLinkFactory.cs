using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;

namespace ClientDashboard_API.Helpers
{
    public class PasswordResetLinkFactory(IConfiguration configuration) : IPasswordResetLinkFactory
    {
        public string Create(PasswordResetToken passwordResetToken)
        {
            var frontendUrl = configuration["FrontendUrl"]
                ?? throw new InvalidOperationException("FrontendUrl is not configured");

            string resetRedirectionLink = $"{frontendUrl}/password-reset?token={passwordResetToken.Id}";

            return resetRedirectionLink;
        }
    }
}
